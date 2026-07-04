using DocsForge.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DocsForge.Settings
{
    internal static class DocsForgePreferencesProvider
    {
        private const string k_PreferencesPath = "Preferences/DocsForge";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(k_PreferencesPath, SettingsScope.User)
            {
                label = "DocsForge",
                activateHandler = OnActivate,
                keywords = new[] { "DocsForge", "documentation", "color", "inspector", "appearance" }
            };
        }

        private static void OnActivate(string searchContext, VisualElement root)
        {
            var prefs = DocsForgePreferences.instance;

            root.style.paddingTop = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            root.Add(BuildSectionLabel("Inspector"));

            var docsPresentColorField = new ColorField("Documented Asset Row Color")
            {
                value = prefs.DocsPresentColor,
                showAlpha = true,
                tooltip = "Background tint shown on the inspector header row when the selected asset has documentation."
            };
            docsPresentColorField.RegisterValueChangedCallback(e => prefs.DocsPresentColor = e.newValue);
            root.Add(docsPresentColorField);

            var delayOpenFloatField = new FloatField("Popup Open Delay (Hover)")
            {
                value = prefs.DocumentationPopupOpenDelay,
                tooltip = "Grace window for how much time header needs to be hovered before popup opens up."
            };
            delayOpenFloatField.RegisterValueChangedCallback(e => prefs.DocumentationPopupOpenDelay = e.newValue);

            var delayCloseFloatField = new FloatField("Popup Close Delay (Hover)")
            {
                value = prefs.DocumentationPopupCloseDelay,
                tooltip = "How long after the cursor leaves the popup or inspector header before the hover popup closes."
            };
            delayCloseFloatField.RegisterValueChangedCallback(e => prefs.DocumentationPopupCloseDelay = e.newValue);

            var popupOpenDirectionEnumField = new EnumField("Popup Open Style", PopupOpenStyle.Under)
            {
                value = prefs.PopupOpenStyle,
                tooltip = "Whether the hover popup opens below the inspector row or to the side of the inspector window."
            };
            popupOpenDirectionEnumField.RegisterValueChangedCallback(e => prefs.PopupOpenStyle = (PopupOpenStyle)e.newValue);

            var hoverOnlyFields = new VisualElement[] { delayOpenFloatField, delayCloseFloatField };

            var popupOpenMethodField = new EnumField("Popup Open Method", PopupOpenMethod.Hover)
            {
                value = prefs.PopupOpenMethod,
                tooltip = "Hover: popup appears automatically after resting the cursor on the inspector header row.\nButton: a dedicated 'Docs' button opens the popup as a focused window that closes when it loses focus."
            };
            popupOpenMethodField.RegisterValueChangedCallback(e =>
            {
                prefs.PopupOpenMethod = (PopupOpenMethod)e.newValue;
                SetHoverFieldsVisible(hoverOnlyFields, (PopupOpenMethod)e.newValue == PopupOpenMethod.Hover);
            });
            root.Add(popupOpenMethodField);

            root.Add(delayOpenFloatField);
            root.Add(delayCloseFloatField);
            root.Add(popupOpenDirectionEnumField);

            SetHoverFieldsVisible(hoverOnlyFields, prefs.PopupOpenMethod == PopupOpenMethod.Hover);
        }

        private static void SetHoverFieldsVisible(VisualElement[] fields, bool visible)
        {
            foreach (var field in fields)
            {
                field.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
