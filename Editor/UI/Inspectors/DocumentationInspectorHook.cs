using System.Collections.Generic;
using System.Linq;
using DocsForge.Core;
using DocsForge.Settings;
using DocsForge.Storage;
using DocsForge.UI.Windows;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DocsForge.UI.Inspectors
{
    internal static partial class DocumentationInspectorHook
    {
        private static DocumentationPopup s_ActivePopup;
        private static string s_PopupIdString;
        private static EditorWindow s_TriggerWindow;
        private static bool s_MouseOverTriggerRect;
        private static double s_CloseScheduledAt = -1;

        private static string s_PendingIdString;
        private static double s_OpenScheduledAt = -1;

        [OnCodeInitializing]
        private static void Initialize()
        {
            Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        [OnCodeDeinitializing]
        private static void Deinitialize()
        {
            Editor.finishedDefaultHeaderGUI -= OnHeaderGUI;
            EditorApplication.update -= CheckPopupClosure;
        }

        private static void OnHeaderGUI(Editor editor)
        {
            if (editor.target == null || editor.target is AssetImporter)
                return;

            if (editor.targets.Length > 1)
            {
                OnMultiTargetHeaderGUI(editor);
                return;
            }

            if (!TryResolveId(editor.target, out var id, out var isSceneObject))
                return;

            if (isSceneObject)
                return;

            var hasDocs = DocumentationStorage.Provider.Exists(id);

            EditorGUILayout.Space(2);
            var groupRect = EditorGUILayout.BeginHorizontal();
            if (hasDocs && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(groupRect, DocsForgePreferences.instance.DocsPresentColor);

            var newHasDocs = EditorGUILayout.ToggleLeft("Documentation", hasDocs);
            if (newHasDocs != hasDocs)
            {
                if (newHasDocs)
                {
                    DocumentationStorage.Provider.Write(id, new AssetDocumentation());
                    DocumentationEditWindow.Open(id);
                }
                else if (EditorUtility.DisplayDialog(
                    "Remove Documentation",
                    "Remove DocsForge documentation from this asset? This cannot be undone.",
                    "Remove", "Cancel"))
                {
                    DocumentationStorage.Provider.Delete(id);
                    ClosePopupForId(id.ToString());
                }
            }

            using (new EditorGUI.DisabledScope(!hasDocs))
            {
                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(36)))
                    DocumentationEditWindow.Open(id);
            }

            if (hasDocs && DocsForgePreferences.instance.PopupOpenMethod == PopupOpenMethod.Button)
            {
                if (GUILayout.Button("Docs", EditorStyles.miniButton, GUILayout.Width(36)))
                {
                    var screenRect = new Rect(GUIUtility.GUIToScreenPoint(groupRect.position), groupRect.size);
                    DocumentationPopup.Show(screenRect, id);
                }
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            if (hasDocs && DocsForgePreferences.instance.PopupOpenMethod == PopupOpenMethod.Hover)
                HandleHoverPopup(id);
        }
        
        private static void OnMultiTargetHeaderGUI(Editor editor)
        {
            var ids = new List<GlobalObjectId>();
            foreach (var target in editor.targets)
            {
                if (target == null)
                    continue;

                if (TryResolveId(target, out var id, out var isSceneObject) && !isSceneObject)
                    ids.Add(id);
            }

            if (ids.Count == 0)
                return;

            var docFlags = ids.Select(id => DocumentationStorage.Provider.Exists(id)).ToList();
            var allHaveDocs = docFlags.All(f => f);
            var noneHaveDocs = docFlags.All(f => !f);

            EditorGUILayout.Space(2);
            var groupRect = EditorGUILayout.BeginHorizontal();
            if (allHaveDocs && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(groupRect, DocsForgePreferences.instance.DocsPresentColor);

            EditorGUI.showMixedValue = !allHaveDocs && !noneHaveDocs;
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.ToggleLeft("Documentation", allHaveDocs);
            var changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;

            if (changed)
            {
                if (newValue)
                {
                    foreach (var id in ids)
                    {
                        if (!DocumentationStorage.Provider.Exists(id))
                            DocumentationStorage.Provider.Write(id, new AssetDocumentation());
                    }
                }
                else if (EditorUtility.DisplayDialog(
                    "Remove Documentation",
                    $"Remove DocsForge documentation from {ids.Count} assets? This cannot be undone.",
                    "Remove", "Cancel"))
                {
                    foreach (var id in ids)
                    {
                        if (!DocumentationStorage.Provider.Exists(id))
                            continue;

                        DocumentationStorage.Provider.Delete(id);
                        ClosePopupForId(id.ToString());
                    }
                }
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        // Returns false when the target cannot be documented (no resolvable ID).
        // isSceneObject is true only for objects that live in a loaded scene.
        // For the root GameObject in prefab stage the ID is remapped to the prefab asset so
        // that the asset view and the prefab-open view share one documentation entry.
        private static bool TryResolveId(Object target, out GlobalObjectId id, out bool isSceneObject)
        {
            // Project asset — fast path.
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)))
            {
                id = GlobalObjectId.GetGlobalObjectIdSlow(target);
                isSceneObject = false;
                return true;
            }

            var go = target as GameObject ?? (target as Component)?.gameObject;
            if (go == null)
            {
                id = default;
                isSceneObject = false;
                return false;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.IsPartOfPrefabContents(go))
            {
                isSceneObject = false;

                // The root GameObject represents the prefab asset itself and shares its doc entry
                if (target is GameObject && go == prefabStage.prefabContentsRoot)
                {
                    var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
                    if (prefabAsset == null)
                    {
                        id = default;
                        return false;
                    }
                    id = GlobalObjectId.GetGlobalObjectIdSlow(prefabAsset);
                    return true;
                }

                id = GlobalObjectId.GetGlobalObjectIdSlow(target);
                return true;
            }

            // Plain scene object.
            isSceneObject = true;
            id = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return true;
        }

        private static void HandleHoverPopup(GlobalObjectId id)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var rect = GUILayoutUtility.GetLastRect();
            var mouseOver = rect.Contains(Event.current.mousePosition);
            var idString = id.ToString();

            if (idString == s_PopupIdString)
                s_MouseOverTriggerRect = mouseOver;

            if (!mouseOver)
            {
                if (s_PendingIdString == idString)
                {
                    s_PendingIdString = null;
                    s_OpenScheduledAt = -1;
                }
                return;
            }

            if (s_PopupIdString == idString && s_ActivePopup != null)
                return;

            if (s_PendingIdString != idString)
            {
                s_PendingIdString = idString;
                s_OpenScheduledAt = EditorApplication.timeSinceStartup + DocsForgePreferences.instance.DocumentationPopupOpenDelay;
            }

            if (EditorApplication.timeSinceStartup < s_OpenScheduledAt)
            {
                EditorWindow.mouseOverWindow?.Repaint();
                return;
            }

            s_PendingIdString = null;
            s_OpenScheduledAt = -1;

            var popup = s_ActivePopup;
            s_ActivePopup = null;
            s_PopupIdString = null;
            s_MouseOverTriggerRect = false;
            popup?.Close();

            s_PopupIdString = idString;
            s_TriggerWindow = EditorWindow.mouseOverWindow;
            s_MouseOverTriggerRect = true;
            s_CloseScheduledAt = -1;

            var screenRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
            s_ActivePopup = DocumentationPopup.Show(screenRect, id);

            EditorApplication.update -= CheckPopupClosure;
            EditorApplication.update += CheckPopupClosure;
        }

        private static void CheckPopupClosure()
        {
            if (s_ActivePopup == null)
            {
                EditorApplication.update -= CheckPopupClosure;
                return;
            }

            var over = EditorWindow.mouseOverWindow;

            if (over == s_ActivePopup || (over == s_TriggerWindow && s_MouseOverTriggerRect))
            {
                s_CloseScheduledAt = -1;
                return;
            }

            if (s_CloseScheduledAt < 0)
            {
                s_CloseScheduledAt = EditorApplication.timeSinceStartup + DocsForgePreferences.instance.DocumentationPopupCloseDelay;
                return;
            }

            if (EditorApplication.timeSinceStartup < s_CloseScheduledAt)
                return;

            CloseActivePopup();
        }

        private static void CloseActivePopup()
        {
            EditorApplication.update -= CheckPopupClosure;

            var popup = s_ActivePopup;
            s_ActivePopup = null;
            s_PopupIdString = null;
            s_TriggerWindow = null;
            s_MouseOverTriggerRect = false;
            s_CloseScheduledAt = -1;

            popup?.Close();
        }

        private static void ClosePopupForId(string idString)
        {
            if (s_PopupIdString == idString)
                CloseActivePopup();

            if (s_PendingIdString == idString)
            {
                s_PendingIdString = null;
                s_OpenScheduledAt = -1;
            }
        }

        /// <summary>Called by <see cref="DocumentationPopup"/> when it is destroyed externally.</summary>
        internal static void NotifyPopupClosed(string idString)
        {
            if (s_PopupIdString != idString)
                return;

            EditorApplication.update -= CheckPopupClosure;
            s_ActivePopup = null;
            s_PopupIdString = null;
            s_TriggerWindow = null;
            s_MouseOverTriggerRect = false;
            s_CloseScheduledAt = -1;
        }
    }
}
