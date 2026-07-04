using System;
using System.Linq;
using System.Text;
using DocsForge.Core;
using DocsForge.UriResolvers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DocsForge.PostProcessors
{
    /// <summary>
    /// Appends prefab structural information to documentation pages: root component list,
    /// root tag and layer, and base prefab reference for prefab variants.
    /// </summary>
    [DocumentationPostProcessor("docsforge.prefab", scope: Scope.Project, displayName: "Prefab Components")]
    public class PrefabDocumentationPostProcessor : IDocumentationPostProcessor
    {
        /// <inheritdoc/>
        public bool AppliesTo(Object target)
        {
            return target is GameObject go 
                   && PrefabUtility.IsPartOfPrefabAsset(go)
                   && go.transform.parent == null
                   && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.Model;
        }

        /// <inheritdoc/>
        public string GenerateAppendix(Object target)
        {
            var prefab = target as GameObject;
            if (prefab == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("## Prefab");

            if (PrefabUtility.IsPartOfVariantPrefab(prefab))
            {
                var baseRoot = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
                if (baseRoot != null && UriResolverRegistry.TryGetResolverByType<AssetUriResolver>(out var resolver) && resolver.TryMakeUri(baseRoot, out var uri))
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Base prefab:** {uri.Markdown}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"**Tag:** {prefab.tag} · **Layer:** {LayerMask.LayerToName(prefab.layer)}");
            sb.AppendLine();
            sb.AppendLine("### Root Components");
            sb.AppendLine();

            foreach (var component in prefab.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                sb.AppendLine($"- {FormatComponentReference(component.GetType())}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// URI link to project-defined component, URL link to Unity native component, plain text for everything else (package, plugins).
        /// </summary>
        private static string FormatComponentReference(Type type)
        {
            if (ProjectTypeCache.GetTypes().Contains(type)
                && UriResolverRegistry.TryGetResolverByType<TypeUriResolver>(out var typeResolver)
                && typeResolver.TryMakeUri(type, out var typeUri))
            {
                return $"[`{type.Name}`]({typeUri.Uri})";
            }

            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal))
                return $"[`{type.Name}`](https://docs.unity3d.com/ScriptReference/{type.Name}.html)";

            return $"`{type.Name}`";
        }
    }
}
