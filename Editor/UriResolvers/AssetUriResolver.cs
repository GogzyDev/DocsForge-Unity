using System;
using DocsForge.Core;
using DocsForge.Storage;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Resolves <c>docsforge://asset/&lt;GlobalObjectId&gt;</c> URIs. Keyed by <see cref="GlobalObjectId"/>
    /// rather than a bare asset GUID so links can target an individual sub-object — a nested asset
    /// sharing one file (e.g. one of several sprites sliced from a texture, or a mesh embedded in an
    /// FBX), a prefab child, or a scene object — instead of only ever resolving to the file's main asset.
    /// On export, produces the output-root-relative path to the generated <c>.md</c> file.
    /// In the editor, pings the target object in the Project window / Hierarchy.
    /// </summary>
    [UriResolver(k_Prefix, k_DisplayName)]
    public class AssetUriResolver : IUriResolver
    {
        public const string k_Prefix = "docsforge://asset/";
        public const string k_DisplayName = "Asset";

        /// <inheritdoc/>
        public void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected)
        {
            var context = SearchService.CreateContext("asset", string.Empty, SearchFlags.None);
            var viewState = new SearchViewState(context)
            {
                windowTitle = new GUIContent("Select Asset"),
                title = "Asset",
                selectHandler = (item, canceled) =>
                {
                    if (canceled || item == null)
                        return;

                    if (item.ToObject() is { } asset && TryMakeUri(asset, out var candidate))
                        onSelected(candidate);
                }
            };

            if (anchor != null)
                viewState.position = anchor.worldBound;

            SearchService.ShowPicker(viewState);
        }

        /// <inheritdoc/>
        public bool TryResolve(string uri, out string output)
        {
            if (!TryGetId(uri, out var id) || !TryResolveTarget(id, out var target))
            {
                output = null;
                return false;
            }

            // Root assets export next to the asset's own path (Assets/Foo/Bar.asset -> Bar.asset.md);
            // sub-objects export as if the parent asset were a folder (MyScene.unity/GameObjectA.md).
            var assetPath = AssetDatabase.GUIDToAssetPath(id.assetGUID);
            var path = AssetDatabase.IsMainAsset(target) ? assetPath : $"{assetPath}/{target.name}";

            // Documented -> link to its generated page.
            // Undocumented -> no page exists; signal the pipeline to emit the path as plain text.
            if (DocumentationStorage.Provider.Exists(id))
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
            if (!TryGetId(uri, out var id) || !TryResolveTarget(id, out var target))
                return false;

            EditorGUIUtility.PingObject(target);
            return true;
        }

        /// <inheritdoc/>
        public bool TryMakeUri(object target, out UriCandidate uriCandidate)
        {
            if (target is not UnityEngine.Object asset || asset == null)
            {
                uriCandidate = default;
                return false;
            }

            var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);
            uriCandidate = new UriCandidate(k_Prefix + id, asset.name);
            return true;
        }

        private static bool TryResolveTarget(GlobalObjectId id, out UnityEngine.Object target)
        {
            target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            return target != null;
        }

        private static bool TryGetId(string uri, out GlobalObjectId id)
        {
            if (uri.StartsWith(k_Prefix, StringComparison.Ordinal))
                return GlobalObjectId.TryParse(uri[k_Prefix.Length..], out id);

            id = default;
            return false;
        }
    }
}
