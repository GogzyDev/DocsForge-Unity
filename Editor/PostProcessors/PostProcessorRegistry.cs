using System;
using System.Collections.Generic;
using System.Linq;
using DocsForge.Core;
using DocsForge.Settings;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DocsForge.PostProcessors
{
    /// <summary>
    /// Central registry for all <see cref="IDocumentationPostProcessor"/> implementations.
    /// Automatically discovers types decorated with <see cref="DocumentationPostProcessorAttribute"/>
    /// via TypeCache on first access. Processors requiring constructor arguments can be registered
    /// manually via <see cref="Register"/>; manually registered processors take precedence over
    /// attribute-discovered ones with the same ID.
    /// </summary>
    public static class PostProcessorRegistry
    {
        public readonly struct PostProcessorEntry
        {
            public readonly IDocumentationPostProcessor Processor;
            public readonly string Id;
            public readonly Scope Scope;
            public readonly string DisplayName;

            public PostProcessorEntry(IDocumentationPostProcessor processor, string id, Scope scope, string displayName)
            {
                Processor = processor;
                Id = id;
                Scope = scope;
                DisplayName = displayName;
            }

            public bool IsEnabledForAssetScopeDocumentation(AssetDocumentation documentation)
            {
                if (Scope != Scope.Asset)
                    return true;
                if (documentation.EnabledAssetScopedProcessors == null)
                    return false;
                return documentation.EnabledAssetScopedProcessors.Contains(Id);
            }
        }

        private static readonly List<PostProcessorEntry> s_Entries = new();
        private static bool s_Discovered;

        /// <summary>
        /// Manually registers a processor. Takes precedence over any attribute-discovered processor
        /// with the same ID. Safe to call before or after auto-discovery runs.
        /// </summary>
        /// <param name="processor">The processor instance to register.</param>
        /// <param name="id">Stable identifier used in opt-in lists and the export cache.</param>
        /// <param name="scope">
        /// <see cref="Scope.Asset"/> the processor only runs for assets where the user has opted in.
        /// <see cref="Scope.Project"/> it runs automatically for all applicable assets unless globally disabled.
        /// </param>
        /// <param name="displayName">Human-readable label shown in Project Settings. Null falls back to <paramref name="id"/>.</param>
        public static void Register(IDocumentationPostProcessor processor, string id, Scope scope = Scope.Project, string displayName = null)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id cannot be null or empty.", nameof(id));

            RemoveById(id);
            s_Entries.Add(new PostProcessorEntry(processor, id, scope, displayName));
            SortEntries();
        }

        /// <summary>
        /// Retrieve all post processors that apply to a given documentation. 
        /// </summary>
        /// <param name="documentation">Target documentation.</param>
        /// <param name="scope">Null for all scopes.</param>
        public static IEnumerable<PostProcessorEntry> GetAllApplicablePostProcessors(AssetDocumentation documentation, Scope? scope)
        {
            EnsureDiscovered();
            return s_Entries
                .Where(e => !scope.HasValue || e.Scope == scope)
                .Where(e => e.Scope != Scope.Project || DocsForgeProjectSettings.instance.IsProcessorEnabled(e.Id))
                .Where(e => e.Processor.AppliesTo(documentation.Target));
        }
        
        /// <summary>
        /// Retrieve all discovered and registered post processors.  
        /// </summary>
        public static IEnumerable<PostProcessorEntry> GetPostProcessors(Scope scope)
        {
            EnsureDiscovered();
            return s_Entries.Where(e => scope == e.Scope);
        }
        

        /// <summary>Clears all entries and resets auto-discovery. For test isolation only.</summary>
        internal static void Reset()
        {
            s_Entries.Clear();
            s_Discovered = false;
        }

        private static void EnsureDiscovered()
        {
            if (s_Discovered)
                return;

            s_Discovered = true;

            foreach (var type in TypeCache.GetTypesWithAttribute<DocumentationPostProcessorAttribute>())
            {
                if (!typeof(IDocumentationPostProcessor).IsAssignableFrom(type))
                    continue;

                var attrs = type.GetCustomAttributes(typeof(DocumentationPostProcessorAttribute), false);
                if (attrs.Length == 0)
                    continue;

                var attr = (DocumentationPostProcessorAttribute)attrs[0];

                if (s_Entries.Any(e => e.Id == attr.Id))
                    continue;

                IDocumentationPostProcessor processor;
                try
                {
                    processor = (IDocumentationPostProcessor)Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                s_Entries.Add(new PostProcessorEntry(processor, attr.Id, attr.Scope, attr.DisplayName));
            }
            SortEntries();
        }

        /// <summary>Sort by arbitrary data for consistent result.</summary>
        private static void SortEntries()
        {
            s_Entries.Sort((el1, el2) => HashCode.Combine(el1.Id).CompareTo(HashCode.Combine(el2.Id)));
        }

        private static void RemoveById(string id)
        {
            for (var i = s_Entries.Count - 1; i >= 0; i--)
            {
                if (s_Entries[i].Id == id)
                    s_Entries.RemoveAt(i);
            }
        }
    }
}
