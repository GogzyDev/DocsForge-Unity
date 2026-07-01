using System;

namespace DocsForge.Core
{
    /// <summary>
    /// Marks a class as a URI resolver and enables automatic discovery via TypeCache.
    /// The decorated class must implement <see cref="IUriResolver"/> and have a public
    /// parameterless constructor.
    /// </summary>
    /// <remarks>
    /// If <see cref="DisplayName"/> is null, the resolver is headless — it handles URIs written
    /// manually in documentation but does not appear in the Insert Link menu and its
    /// <see cref="IUriResolver.OpenPicker"/> is never called by the registry.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UriResolverAttribute : Attribute
    {
        /// <summary>The URI prefix this resolver handles, e.g. <c>docsforge://asset/</c>.</summary>
        public string Prefix { get; }

        /// <summary>
        /// Human-readable label shown in the Insert Link menu, e.g. <c>"Asset"</c> or <c>"Type"</c>.
        /// Null for headless resolvers that are not reachable from the UI.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>Initializes a headless resolver attribute that handles URIs but is not reachable from the UI.</summary>
        public UriResolverAttribute(string prefix)
        {
            Prefix = prefix;
        }

        /// <summary>Initializes a UI-visible resolver attribute with the given URI prefix and Insert Link menu label.</summary>
        public UriResolverAttribute(string prefix, string displayName)
        {
            Prefix = prefix;
            DisplayName = displayName;
        }
    }
}
