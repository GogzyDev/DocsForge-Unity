using System;
using DocsForge.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Resolves <c>docsforge://type/&lt;FullyQualifiedName&gt;</c> URIs.
    /// On export, produces the DocFX xref syntax <c>@FullyQualified.Name</c>.
    /// In the editor, opens the corresponding script file in the IDE.
    /// </summary>
    [UriResolver(k_Prefix, k_DisplayName)]
    public class TypeUriResolver : IUriResolver
    {
        public const string k_Prefix = "docsforge://type/";
        public const string k_DisplayName = "Type";

        /// <inheritdoc/>
        public void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected) { }

        /// <inheritdoc/>
        public bool TryResolve(string uri, out string output)
        {
            if (!TryGetTypeName(uri, out var typeName))
            {
                output = null;
                return false;
            }

            output = $"@{typeName}";
            return true;
        }

        /// <inheritdoc/>
        public bool TryOpenInEditor(string uri)
        {
            if(!TryGetTypeName(uri, out var typeName))
                return false;

            // Use the simple name for the AssetDatabase search, then confirm by class name.
            var simpleName = typeName.Contains('.')
                ? typeName[(typeName.LastIndexOf('.') + 1)..]
                : typeName;

            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {simpleName}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var scriptClass = script?.GetClass();
                if (scriptClass == null || GetUriCompatibleTypeName(scriptClass) != typeName)
                    continue;
                AssetDatabase.OpenAsset(script);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool TryMakeUri(object target, out UriCandidate uriCandidate)
        {
            if (target is not Type type)
            {
                uriCandidate = default;
                return false;
            }
            uriCandidate = new UriCandidate(k_Prefix + GetUriCompatibleTypeName(type), type.Name);
            return true;
        }

        private static string GetUriCompatibleTypeName(Type type) =>
            type.FullName?.Replace('+', '.').Replace('`', '-');

        private static bool TryGetTypeName(string uri, out string typeName)
        {
            if (uri.StartsWith(k_Prefix, StringComparison.Ordinal))
                return !string.IsNullOrEmpty(typeName = uri[k_Prefix.Length..]);
            typeName = null;
            return false;

        }
    }
}
