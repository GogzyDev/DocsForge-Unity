using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocsForge.Core;
using UnityEditor;
using UnityEditor.Compilation;
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
        public void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected)
        {
            var dropdown = new TypePickerDropdown(type =>
            {
                if (TryMakeUri(type, out var candidate))
                    onSelected(candidate);
            });
            dropdown.Show(anchor.worldBound);
        }

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
            if (!TryGetTypeName(uri, out var typeName))
                return false;

            return TryOpenByFileNameMatch(typeName) || TryOpenByAssemblySourceScan(typeName);
        }

        // Fast path: works whenever the declaring file's name contains the type's simple name,
        // which covers the common one-type-per-file convention without touching CompilationPipeline.
        private static bool TryOpenByFileNameMatch(string typeName)
        {
            var simpleName = typeName.Contains('.')
                ? typeName[(typeName.LastIndexOf('.') + 1)..]
                : typeName;

            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {simpleName}"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (TryOpenIfMatches(script, typeName))
                    return true;
            }

            return false;
        }

        // Fallback for types whose declaring file name doesn't match the type's simple name
        // (multiple types per file, nested types), where MonoScript.GetClass() can't help either
        // since it only ever resolves one type per script. Resolves the type via ProjectTypeCache
        // to find its assembly, then greps every source file compiled into that assembly for an
        // actual type declaration. Best-effort text match, not a real parse: it can't see through
        // comments/strings containing the same declaration text, but that's an acceptable tradeoff
        // for a fallback that only runs when the fast path already failed.
        private static bool TryOpenByAssemblySourceScan(string typeName)
        {
            var type = ProjectTypeCache.GetTypes().FirstOrDefault(t => GetUriCompatibleTypeName(t) == typeName);
            if (type == null)
                return false;

            var assemblyName = type.Assembly.GetName().Name;
            var sourceFiles = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .FirstOrDefault(a => a.name == assemblyName)?.sourceFiles;

            if (sourceFiles == null)
                return false;

            // Generic types carry a `N arity suffix (e.g. Foo`1 for Foo<T>) that never appears in source.
            var simpleName = type.Name.Contains('`') ? type.Name[..type.Name.IndexOf('`')] : type.Name;
            var declarationPattern = new Regex($@"\b(class|struct|interface|enum|record)\s+{Regex.Escape(simpleName)}\b");

            foreach (var path in sourceFiles)
            {
                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch (IOException)
                {
                    continue;
                }

                if (!declarationPattern.IsMatch(text))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                    continue;

                AssetDatabase.OpenAsset(script);
                return true;
            }

            return false;
        }

        private static bool TryOpenIfMatches(MonoScript script, string typeName)
        {
            var scriptClass = script?.GetClass();
            if (scriptClass == null || GetUriCompatibleTypeName(scriptClass) != typeName)
                return false;

            AssetDatabase.OpenAsset(script);
            return true;
        }

        /// <inheritdoc/>
        public bool TryMakeUri(object target, out UriCandidate uriCandidate)
        {
            // A dragged-and-dropped MonoScript asset resolves to whatever type it defines, same
            // as picking that type directly — this only works for the one type GetClass() can
            // resolve per script, same limitation as TryOpenByFileNameMatch above.
            var type = target switch
            {
                Type t => t,
                MonoScript script => script.GetClass(),
                _ => null
            };

            if (type == null)
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
