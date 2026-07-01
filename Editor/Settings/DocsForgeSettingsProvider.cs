using System.Linq;
using DocsForge.Core;
using DocsForge.PostProcessors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DocsForge.Settings
{
    internal static class DocsForgeSettingsProvider
    {
        private const string k_SettingsPath = "Project/DocsForge";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(k_SettingsPath, SettingsScope.Project)
            {
                label = "DocsForge",
                activateHandler = OnActivate,
                keywords = new[] { "DocsForge", "documentation", "docfx", "export", "markdown" }
            };
        }

        private static void OnActivate(string searchContext, VisualElement root)
        {
            var settings = DocsForgeProjectSettings.instance;

            root.style.paddingTop = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            // Post-processors
            root.Add(BuildSectionLabel("Post-Processors"));

            root.Add(new HelpBox(
                "Project-scoped processors automatically append generated content (such as Addressables metadata, " +
                "prefab component lists, or serialized field layouts) to documentation pages. " +
                "Disabling a processor here suppresses it for all assets project-wide without affecting stored documentation content.",
                HelpBoxMessageType.Info));

            var processors = PostProcessorRegistry.GetPostProcessors(Scope.Project).ToArray();
            if (processors.Length == 0)
            {
                root.Add(new Label("No processors registered.") { style = { color = new Color(0.6f, 0.6f, 0.6f), marginTop = 4 } });
            }
            else
            {
                foreach (var p in processors)
                {
                    var capturedId = p.Id;
                    var toggle = new Toggle($"Enable {p.DisplayName}")
                    {
                        value = settings.IsProcessorEnabled(capturedId),
                        tooltip = $"When disabled, the '{p.DisplayName}' processor is suppressed for all assets project-wide."
                    };
                    toggle.RegisterValueChangedCallback(e => settings.SetProcessorEnabled(capturedId, e.newValue));
                    root.Add(toggle);
                }
            }
        }

        private static Label BuildSectionLabel(string text)
        {
            return new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 12,
                    marginBottom = 4
                }
            };
        }
    }
}
