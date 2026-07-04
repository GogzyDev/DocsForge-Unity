using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocsForge.Core;
using DocsForge.PostProcessors;
using DocsForge.Settings;
using DocsForge.Storage;
using DocsForge.UriResolvers;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DocsForge.UI.Windows
{
    /// <summary>
    /// Editor window for authoring asset documentation.
    /// </summary>
    internal class DocumentationEditWindow : EditorWindow
    {
        private const string k_UxmlPath = "Packages/com.gogzydev.docsforge/Editor/UI/Windows/DocumentationEditWindow.uxml";
        private const string k_UnsavedChangesMessage = "You have unsaved documentation changes. Do you want to save them?";
        private const string k_InsertLinkShortcutId = "DocsForge/Insert Link";

        [SerializeField] private string m_SerializedId;

        private GlobalObjectId m_Id;
        private AssetDocumentation m_Doc;

        private TabView m_TabView;
        private Tab m_TabEdit;
        private Tab m_TabPreview;
        private TextField m_ContentField;
        private ScrollView m_PreviewScroll;
        private Label m_InfoAssetPath;
        private Label m_InfoGlobalId;
        private VisualElement m_SectionAssetProcessors;
        private VisualElement m_SectionProjectProcessors;
        private Button m_SaveBtn;
        private Button m_InsertLinkBtn;
        private Label m_InsertLinkShortcutLabel;
        private int m_LastCaretIndex;

        /// <summary>Opens the edit window for the object identified by <paramref name="id"/>, reusing an existing window if one is open.</summary>
        public static void Open(GlobalObjectId id)
        {
            var window = GetWindow<DocumentationEditWindow>();
            window.LoadForId(id);
        }

        private void CreateGUI()
        {
            minSize = new Vector2(420, 480);

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            uxml?.CloneTree(rootVisualElement);

            m_TabView    = rootVisualElement.Q<TabView>("tab-view");
            m_TabEdit    = rootVisualElement.Q<Tab>("tab-edit");
            m_TabPreview = rootVisualElement.Q<Tab>("tab-preview");
            m_ContentField  = rootVisualElement.Q<TextField>("content-field");
            m_PreviewScroll = rootVisualElement.Q<ScrollView>("preview-scroll");
            m_InfoAssetPath = rootVisualElement.Q<Label>("info-asset-path");
            m_InfoGlobalId  = rootVisualElement.Q<Label>("info-global-id");
            m_SectionAssetProcessors   = rootVisualElement.Q("section-asset-processors");
            m_SectionProjectProcessors = rootVisualElement.Q("section-project-processors");
            m_SaveBtn = rootVisualElement.Q<Button>("btn-save");
            m_InsertLinkBtn = rootVisualElement.Q<Button>("btn-insert-link");
            m_InsertLinkShortcutLabel = rootVisualElement.Q<Label>("lbl-insert-link-shortcut");

            m_TabView.activeTabChanged += (_, newTab) =>
            {
                if (newTab == m_TabPreview)
                    BuildPreview();
            };

            m_InsertLinkBtn.clicked += OnInsertLink;
            m_SaveBtn.clicked += SaveChanges;
            m_SaveBtn.SetEnabled(false);

            m_ContentField.selectAllOnFocus = false;
            m_ContentField.selectAllOnMouseUp = false;
            m_ContentField.RegisterValueChangedCallback(e =>
            {
                if (m_Doc != null)
                {
                    m_Doc.Content = e.newValue;
                    SetUnsavedChanges(true);
                }
            });

            m_ContentField.RegisterCallback<FocusOutEvent>(_ => m_LastCaretIndex = m_ContentField.cursorIndex);

            m_ContentField.RegisterCallback<DragUpdatedEvent>(OnContentFieldDragUpdated);
            m_ContentField.RegisterCallback<DragPerformEvent>(OnContentFieldDragPerform);

            ShortcutManager.instance.shortcutBindingChanged -= OnShortcutBindingChanged;
            ShortcutManager.instance.shortcutBindingChanged += OnShortcutBindingChanged;
            UpdateInsertLinkShortcutLabel();

            if (!string.IsNullOrEmpty(m_SerializedId) && GlobalObjectId.TryParse(m_SerializedId, out var id))
                LoadForId(id);
        }

        private void OnDisable()
        {
            ShortcutManager.instance.shortcutBindingChanged -= OnShortcutBindingChanged;
        }

        private void LoadForId(GlobalObjectId id)
        {
            m_Id = id;
            m_SerializedId = id.ToString();

            var existing = DocumentationStorage.Provider.Read(id);
            if (existing != null)
            {
                m_Doc = existing;
            }
            else
            {
                m_Doc = new AssetDocumentation();
                m_Doc.Initialize(id);
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(id.assetGUID);
            titleContent = new GUIContent($"DocsForge - {Path.GetFileName(assetPath)}");
            saveChangesMessage = k_UnsavedChangesMessage;
            SetUnsavedChanges(false);

            Refresh();
        }

        private void Refresh()
        {
            if (m_ContentField == null)
                return;

            m_ContentField.SetValueWithoutNotify(m_Doc?.Content ?? string.Empty);
            m_TabView.activeTab = m_TabEdit;
            BuildInfoPanel();
        }

        #region  Preview Tab Logic
        private void BuildPreview()
        {
            m_PreviewScroll.Clear();

            if (m_Doc == null)
                return;

            DocumentationContentView.Render(m_PreviewScroll, m_Doc);
        }
        #endregion

        #region Info Tab Logic
        private void BuildInfoPanel()
        {
            if (m_InfoAssetPath == null)
                return;

            m_InfoAssetPath.text = AssetDatabase.GUIDToAssetPath(m_Id.assetGUID);
            m_InfoGlobalId.text  = m_Id.ToString();

            BuildAssetProcessorToggles();
            BuildProjectProcessorList();
        }

        private void BuildAssetProcessorToggles()
        {
            while (m_SectionAssetProcessors.childCount > 1)
                m_SectionAssetProcessors.RemoveAt(1);

            var applicable = PostProcessorRegistry
                .GetAllApplicablePostProcessors(m_Doc, Scope.Asset)
                .ToArray();

            if (applicable.Length == 0)
            {
                m_SectionAssetProcessors.Add(MakeMutedLabel("No asset-scoped processors apply to this asset."));
                return;
            }

            foreach (var entry in applicable)
            {
                var capturedId = entry.Id;
                var isEnabled  = m_Doc?.EnabledAssetScopedProcessors?.Contains(capturedId) ?? false;

                var toggle = new Toggle(entry.DisplayName ?? entry.Id) { value = isEnabled };
                toggle.RegisterValueChangedCallback(e =>
                {
                    SetProcessorEnabled(capturedId, e.newValue);
                    SetUnsavedChanges(true);
                });
                m_SectionAssetProcessors.Add(toggle);
            }
        }

        private void BuildProjectProcessorList()
        {
            while (m_SectionProjectProcessors.childCount > 1)
                m_SectionProjectProcessors.RemoveAt(1);

            var applicable = PostProcessorRegistry
                .GetAllApplicablePostProcessors(m_Doc, Scope.Project)
                .ToArray();

            if (applicable.Length == 0)
            {
                m_SectionProjectProcessors.Add(MakeMutedLabel("No project-scoped processors apply to this asset."));
            }
            else
            {
                foreach (var entry in applicable)
                {
                    var row = new VisualElement();
                    row.AddToClassList("docsforge-edit-window__processor-row");

                    var nameLabel = new Label(entry.DisplayName ?? entry.Id);
                    nameLabel.AddToClassList("docsforge-edit-window__processor-name");

                    var globallyEnabled = DocsForgeProjectSettings.instance.IsProcessorEnabled(entry.Id);
                    var statusLabel = new Label(globallyEnabled ? "Enabled" : "Disabled globally");
                    statusLabel.AddToClassList("docsforge-edit-window__processor-status");
                    if (!globallyEnabled)
                        statusLabel.AddToClassList("docsforge-edit-window__processor-status--disabled");

                    row.Add(nameLabel);
                    row.Add(statusLabel);
                    m_SectionProjectProcessors.Add(row);
                }
            }

            var openSettingsBtn = new Button(() => SettingsService.OpenProjectSettings("Project/DocsForge"))
            {
                text = "Open Project Settings"
            };
            openSettingsBtn.AddToClassList("docsforge-edit-window__open-settings-btn");
            m_SectionProjectProcessors.Add(openSettingsBtn);
        }
        #endregion

        #region Save / Discard
        /// <inheritdoc/>
        public override void SaveChanges()
        {
            if (string.IsNullOrEmpty(m_SerializedId) || m_Doc == null)
                return;

            DocumentationStorage.Provider.Write(m_Id, m_Doc);
            base.SaveChanges();
            SetUnsavedChanges(false);
        }

        /// <inheritdoc/>
        public override void DiscardChanges()
        {
            if (!string.IsNullOrEmpty(m_SerializedId))
            {
                var existing = DocumentationStorage.Provider.Read(m_Id);
                if (existing != null)
                {
                    m_Doc = existing;
                }
                else
                {
                    m_Doc = new AssetDocumentation();
                    m_Doc.Initialize(m_Id);
                }

                Refresh();
            }

            base.DiscardChanges();
            SetUnsavedChanges(false);
        }
        #endregion
        
        #region Helpers
        private void SetUnsavedChanges(bool value)
        {
            hasUnsavedChanges = value;
            m_SaveBtn?.SetEnabled(value);
        }

        private void SetProcessorEnabled(string id, bool enabled)
        {
            if (m_Doc == null)
                return;

            var list = m_Doc.EnabledAssetScopedProcessors?.ToList() ?? new List<string>();

            if (enabled && !list.Contains(id))
                list.Add(id);
            else if (!enabled)
                list.Remove(id);

            m_Doc.EnabledAssetScopedProcessors = list.ToArray();
        }
        
        private static Label MakeMutedLabel(string text)
        {
            var label = new Label(text);
            label.SetEnabled(false);
            return label;
        }
        #endregion
        
        #region Insert Link logic
        private void OnInsertLink()
        {
            if (m_TabView.activeTab != m_TabEdit) // shortcut can be performed in other tabs, bale out early
                return;
            
            if (m_ContentField.focusController?.focusedElement == m_ContentField)
                m_LastCaretIndex = m_ContentField.cursorIndex;

            var menu = new GenericMenu();
            foreach (var (displayName, resolver) in UriResolverRegistry.GetUiResolvers())
                menu.AddItem(new GUIContent(displayName), false, () => resolver.OpenPicker(m_InsertLinkBtn, OnLinkCandidateSelected));

            menu.DropDown(m_InsertLinkBtn.worldBound);
        }

        private void OnLinkCandidateSelected(UriCandidate candidate)
        {
            var content = m_ContentField.value ?? string.Empty;
            var caret = Mathf.Clamp(m_LastCaretIndex, 0, content.Length);

            var needsLeadingSpace = caret > 0 && !char.IsWhiteSpace(content[caret - 1]);
            var insertion = (needsLeadingSpace ? " " : string.Empty) + candidate.Markdown;

            m_ContentField.value = content[..caret] + insertion + content[caret..];
            m_LastCaretIndex = caret + insertion.Length;
            
            EditorApplication.delayCall += () =>
            {
                if (m_ContentField?.panel == null)
                    return;

                Focus();
                m_ContentField.Focus();
                m_ContentField.SelectRange(m_LastCaretIndex, m_LastCaretIndex);
            };
        }
        
        private void OnContentFieldDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDrop.objectReferences.Length == 1
                ? DragAndDropVisualMode.Link
                : DragAndDropVisualMode.Rejected;
        }

        private void OnContentFieldDragPerform(DragPerformEvent evt)
        {
            var objects = DragAndDrop.objectReferences;
            if (objects.Length != 1)
                return;

            DragAndDrop.AcceptDrag();

            var target = objects[0];
            var matches = new List<(string DisplayName, UriCandidate Candidate)>();

            foreach (var (displayName, resolver) in UriResolverRegistry.GetUiResolvers())
            {
                if (resolver.TryMakeUri(target, out var candidate))
                    matches.Add((displayName, candidate));
            }

            switch (matches.Count)
            {
                case 0:
                    return;
                case 1:
                    OnLinkCandidateSelected(matches[0].Candidate);
                    break;
                default:
                    ShowLinkDisambiguationMenu(matches, evt.localMousePosition);
                    break;
            }
        }

        /// <summary>
        /// If several resolvers claim the same dropped object (e.g. MonoScript is both a linkable asset and type it defines)
        /// let the user pick which link they meant.
        /// </summary>
        private void ShowLinkDisambiguationMenu(List<(string DisplayName, UriCandidate Candidate)> matches, Vector2 localDropPosition)
        {
            var menu = new GenericMenu();
            foreach (var (displayName, candidate) in matches)
                menu.AddItem(new GUIContent(displayName), false, () => OnLinkCandidateSelected(candidate));

            var dropPoint = m_ContentField.LocalToWorld(localDropPosition);
            menu.DropDown(new Rect(dropPoint, Vector2.zero));
        }

        [Shortcut(k_InsertLinkShortcutId, typeof(DocumentationEditWindow), KeyCode.K, ShortcutModifiers.Action)]
        private static void InvokeInsertLinkShortcut(ShortcutArguments args)
        {
            if (args.context is DocumentationEditWindow window)
                window.OnInsertLink();
        }

        private void OnShortcutBindingChanged(ShortcutBindingChangedEventArgs args)
        {
            if (args.shortcutId == k_InsertLinkShortcutId)
                UpdateInsertLinkShortcutLabel();
        }

        private void UpdateInsertLinkShortcutLabel()
        {
            if (m_InsertLinkShortcutLabel == null)
                return;

            var binding = ShortcutManager.instance.GetShortcutBinding(k_InsertLinkShortcutId);
            m_InsertLinkShortcutLabel.text = binding.Equals(ShortcutBinding.empty) ? string.Empty : $"({binding})";
        }
        #endregion
    }
}
