using DocsForge.Core;
using DocsForge.Settings;
using DocsForge.Storage;
using DocsForge.UI.Inspectors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DocsForge.UI.Windows
{
    /// <summary>
    /// Popup window showing rendered documentation for an asset.
    /// </summary>
    internal class DocumentationPopup : EditorWindow
    {
        private const string k_UxmlPath = "Packages/com.gogzydev.docsforge/Editor/UI/Windows/DocumentationPopup.uxml";

        private GlobalObjectId m_Id;
        
        /// <summary>
        /// Shows a documentation popup anchored to <paramref name="triggerRectScreen"/>.
        /// Open mode and position are determined by <see cref="DocsForgePreferences"/>.
        /// </summary>
        public static DocumentationPopup Show(Rect triggerRectScreen, GlobalObjectId id)
        {
            var popup = CreateInstance<DocumentationPopup>();
            popup.m_Id = id;

            Rect targetRect;
            switch (DocsForgePreferences.instance.PopupOpenStyle)
            {
                case PopupOpenStyle.Under:
                    targetRect = new Rect(triggerRectScreen.x, triggerRectScreen.yMax, triggerRectScreen.width, 280);
                    break;
                case PopupOpenStyle.OnTheSide:
                    var windowBounds = EditorGUIUtility.GetMainWindowPosition();
                    var distLeft  = triggerRectScreen.x - windowBounds.x;
                    var distRight = windowBounds.xMax - triggerRectScreen.xMax;
                    var onLeft    = distLeft < distRight;
                    var width     = Mathf.Min(triggerRectScreen.width, onLeft ? distRight : distLeft);
                    targetRect = new Rect(
                        onLeft ? triggerRectScreen.xMax : triggerRectScreen.x - width,
                        triggerRectScreen.y,
                        width, 280);
                    break;
                default:
                    targetRect = new Rect(triggerRectScreen.x, triggerRectScreen.yMax, triggerRectScreen.width, 280);
                    break;
            }

            switch (DocsForgePreferences.instance.PopupOpenMethod)
            {
                case PopupOpenMethod.Hover:
                    popup.wantsMouseEnterLeaveWindow = true;
                    popup.position = targetRect;
                    popup.ShowPopup();
                    return popup;
                case PopupOpenMethod.Button:
                    var dropdownRect = new Rect(targetRect.x, targetRect.y - 10, targetRect.width, 10);
                    popup.ShowAsDropDown(dropdownRect, new Vector2(targetRect.width, targetRect.height));
                    return popup;
                default:
                    popup.Show();
                    return popup;
            }
        }
        
        private void CreateGUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            uxml?.CloneTree(rootVisualElement);

            var scroll = rootVisualElement.Q<ScrollView>("scroll");
            if (scroll == null)
                return;

            var doc = DocumentationStorage.Provider.Read(m_Id);
            if (doc == null)
                return;

            DocumentationContentView.Render(scroll, doc);
        }

        private void OnDestroy()
        {
            DocumentationInspectorHook.NotifyPopupClosed(m_Id.ToString());
        }
    }
}
