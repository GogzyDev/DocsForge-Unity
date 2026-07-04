using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.Compilation;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Caches every documentable type defined in an assembly compiled from sources under
    /// <c>Assets/</c> (including the default Assembly-CSharp assemblies). Reflection-based rather
    /// than MonoScript-based so multiple types per file, nested types, and non-behaviour types
    /// (structs, interfaces, enums) are all discoverable. Shared by <see cref="TypePickerDropdown"/>
    /// and by <see cref="TypeUriResolver"/>'s fallback lookup for types with no matching MonoScript filename.
    /// </summary>
    internal static class ProjectTypeCache
    {
        // Names injected by Unity's own post-compile tooling rather than the C# compiler, so they
        // carry no [CompilerGenerated] attribute and no generic name-mangling signal either.
        private static readonly HashSet<string> s_KnownInjectedTypeNames = new() { "MonoScriptData" };

        private static Type[] s_Types;

        // Cached for the domain's lifetime: project assemblies only change via script recompile,
        // which itself triggers a domain reload and resets this field.
        public static IEnumerable<Type> GetTypes() => s_Types ??= ComputeTypes();

        private static Type[] ComputeTypes()
        {
            var projectAssemblyNames = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .Where(a => a.sourceFiles.Any(f => f.Replace('\\', '/').StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)))
                    .Select(a => a.name));

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => projectAssemblyNames.Contains(a.GetName().Name))
                .SelectMany(GetLoadableTypes)
                .Where(IsDocumentableType)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        // Assembly.GetTypes() also surfaces compiler-generated noise (closures, state machines,
        // Roslyn's <PrivateImplementationDetails> array-init helpers, etc.). FullName (not Name)
        // is checked for mangling so nested synthetic types are caught via their container's name.
        private static bool IsDocumentableType(Type type) =>
            type.FullName?.Contains('<') != true
            && !type.Name.StartsWith("__", StringComparison.Ordinal)
            && !Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))
            && !s_KnownInjectedTypeNames.Contains(type.Name);
    }
}
