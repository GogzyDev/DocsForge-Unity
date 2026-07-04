using System.IO;
using DocsForge.Core;
using UnityEditor;
using UnityEngine;

namespace DocsForge.Settings
{
    /// <summary>Per-user DocsForge preferences, persisted to <c>UserSettings/DocsForge.preferences</c>.</summary>
    [FilePath("UserSettings/DocsForge.preferences", FilePathAttribute.Location.ProjectFolder)]
    public class DocsForgePreferences : ScriptableSingleton<DocsForgePreferences>
    {
        [SerializeField] private Color m_DocsPresentColor = new Color(0.35f, 0.75f, 0.65f, 0.3f);
        [SerializeField] private float m_DocumentationPopupOpenDelay = 0.3f;
        [SerializeField] private float m_DocumentationPopupCloseDelay = 0.15f;
        [SerializeField] private PopupOpenStyle m_PopupOpenStyle = PopupOpenStyle.Under;
        [SerializeField] private PopupOpenMethod m_PopupOpenMethod = PopupOpenMethod.Hover;

        /// <summary>Background tint drawn on the inspector header row when the selected asset has documentation.</summary>
        public Color DocsPresentColor
        {
            get => m_DocsPresentColor;
            set { m_DocsPresentColor = value; Save(true); }
        }

        /// <summary>Grace window for how much time header needs to be hovered before popup opens up.</summary>
        public float DocumentationPopupOpenDelay
        {
            get => m_DocumentationPopupOpenDelay;
            set { m_DocumentationPopupOpenDelay = value; Save(true); }
        }

        /// <summary>Grace window for after how much time popup closes when its no longer hovered.</summary>
        public float DocumentationPopupCloseDelay
        {
            get => m_DocumentationPopupCloseDelay;
            set { m_DocumentationPopupCloseDelay = value; Save(true); }
        }

        /// <summary>In what direction documentation popup opens.</summary>
        public PopupOpenStyle PopupOpenStyle
        {
            get => m_PopupOpenStyle;
            set { m_PopupOpenStyle = value; Save(true); }
        }

        /// <summary>Whether the documentation popup is triggered by hovering the inspector header or by clicking a dedicated button.</summary>
        public PopupOpenMethod PopupOpenMethod
        {
            get => m_PopupOpenMethod;
            set { m_PopupOpenMethod = value; Save(true); }
        }

        /// <summary>Deletes the settings asset from disk. Called during package removal cleanup.</summary>
        internal static void DeleteAsset()
        {
            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "UserSettings", "DocsForge.preferences"));

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
