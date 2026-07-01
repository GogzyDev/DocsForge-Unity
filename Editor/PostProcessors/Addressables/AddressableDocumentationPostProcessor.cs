using System.Linq;
using System.Text;
using DocsForge.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using Object = UnityEngine.Object;

namespace DocsForge.PostProcessors
{
    /// <summary>
    /// Appends Addressables metadata to documentation pages for assets that are registered
    /// in the Addressables catalog: address key, group name, and assigned labels.
    /// Only compiled when the <c>com.unity.addressables</c> package is present in the project.
    /// </summary>
    [DocumentationPostProcessor("docsforge.addressables", scope: Scope.Project, displayName: "Addressables Metadata")]
    public class AddressableDocumentationPostProcessor : IDocumentationPostProcessor
    {
        /// <inheritdoc/>
        public bool AppliesTo(Object target)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;
            
            var path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path))
                return false;
            
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return false;
            
            return settings.FindAssetEntry(guid) != null;
        }

        /// <inheritdoc/>
        public string GenerateAppendix(Object target)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return string.Empty;

            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target)));
            if (entry == null)
                return string.Empty;

            var labelText = entry.labels.Count > 0
                ? string.Join(", ", entry.labels.OrderBy(l => l).Select(l => $"`{l}`"))
                : "—";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("## Addressables");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Key | `{entry.address}` |");
            sb.AppendLine($"| Group | {entry.parentGroup.Name} |");
            sb.AppendLine($"| Labels | {labelText} |");

            return sb.ToString();
        }
    }
}
