using System;
using DocsForge.Core;
using DocsForge.Storage;
using UnityEditor;
using UnityEngine.UIElements;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Resolves <c>docsforge://asset/&lt;guid&gt;</c> URIs.
    /// On export, produces the output-root-relative path to the generated <c>.md</c> file.
    /// In the editor, pings the target asset in the Project window.
    /// </summary>
    [UriResolver(k_Prefix, k_DisplayName)]
    public class AssetUriResolver : IUriResolver
    {
        public const string k_Prefix = "docsforge://asset/";
        public const string k_DisplayName = "Asset";

        /// <inheritdoc/>
        public void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected) { }

        /// <inheritdoc/>
        public bool TryResolve(string uri, out string output)
        {
            if (!TryGetPathAndGuid(uri, out var path, out var guid))
            {
                output = null;
                return false;
            }

            // Documented asset -> link to its generated page.
            // Undocumented asset -> no page exists; signal the pipeline to emit the path as plain text.
            if (DocumentationStorage.Provider.Exists(guid))
            {
                output = path + ".md";
                return true;
            }

            output = path;
            return false;
        }

        /// <inheritdoc/>
        public bool TryOpenInEditor(string uri)
        {
            if(!TryGetPathAndGuid(uri, out var path, out var guid))
                return false;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                return false;

            EditorGUIUtility.PingObject(asset);
            return true;
        }

        /// <inheritdoc/>
        public bool TryMakeUri(object target, out UriCandidate uriCandidate)
        {
            if (target is not UnityEngine.Object asset)
            {
                uriCandidate = default;
                return false;
            }
            var path = AssetDatabase.GetAssetPath(asset);
            if (path == null || string.IsNullOrEmpty(path))
            {
                uriCandidate = default;
                return false;
            }
            var guid = AssetDatabase.AssetPathToGUID(path);
            uriCandidate = new UriCandidate(k_Prefix + guid, asset.name);
            return true;
        }

        private static bool TryGetPathAndGuid(string uri, out string path, out string guid)
        {
            if (!uri.StartsWith(k_Prefix, StringComparison.Ordinal))
            {
                path = null;
                guid = null;
                return false;
            }

            guid = uri[k_Prefix.Length..];
            path = AssetDatabase.GUIDToAssetPath(guid);

            return !string.IsNullOrEmpty(path);
        }
    }
}
