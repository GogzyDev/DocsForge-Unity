using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DocsForge.Settings
{
    /// <summary>Project-wide DocsForge settings, persisted to <c>ProjectSettings/DocsForge.asset</c>.</summary>
    [FilePath("ProjectSettings/DocsForge.asset", FilePathAttribute.Location.ProjectFolder)]
    public class DocsForgeProjectSettings : ScriptableSingleton<DocsForgeProjectSettings>
    {
        [SerializeField] private List<string> m_DisabledProjectScopedProcessorIds = new();

        /// <summary>Returns true if the project-scoped processor with the given ID is enabled.</summary>
        public bool IsProcessorEnabled(string id) =>
            !m_DisabledProjectScopedProcessorIds.Contains(id);

        /// <summary>Enables or disables the project-scoped processor with the given ID globally.</summary>
        public void SetProcessorEnabled(string id, bool enabled)
        {
            if (enabled)
                m_DisabledProjectScopedProcessorIds.Remove(id);
            else if (!m_DisabledProjectScopedProcessorIds.Contains(id))
                m_DisabledProjectScopedProcessorIds.Add(id);

            Save(true);
        }

        /// <summary>Deletes the settings asset from disk. Called during package removal cleanup.</summary>
        internal static void DeleteAsset()
        {
            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "DocsForge.asset"));

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
