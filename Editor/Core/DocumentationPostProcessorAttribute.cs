using System;

namespace DocsForge.Core
{
    /// <summary>
    /// Marks a class as a documentation post-processor and enables automatic discovery via
    /// TypeCache. The decorated class must implement <see cref="IDocumentationPostProcessor"/>
    /// and have a public parameterless constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DocumentationPostProcessorAttribute : Attribute
    {
        /// <summary>Stable identifier for this processor, used in opt-in lists and the export cache.</summary>
        public string Id { get; }

        /// <summary>
        /// Determines if post processor is selectable on per-asset basis, or runs for all matching assets.
        /// </summary>
        public Scope Scope { get; }

        /// <summary>
        /// Human-readable label shown in the Project Settings processor list.
        /// When null, the <see cref="Id"/> is used as a fallback label.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>Initializes the attribute with the given id, scope, and optional display name.</summary>
        public DocumentationPostProcessorAttribute(string id, Scope scope = Scope.Project, string displayName = null)
        {
            Id = id;
            Scope = scope;
            DisplayName = displayName;
        }
    }
}
