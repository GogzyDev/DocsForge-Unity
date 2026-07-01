using System;
using DocsForge.Core;

namespace DocsForge.Storage
{
    /// <summary>
    /// Global access point for the active <see cref="IDocumentationStorage"/> provider.
    /// Defaults to <see cref="MetaFileDocumentationStorage"/>; replace via <see cref="SetProvider"/>
    /// to swap in a custom backend without changing any calling code.
    /// </summary>
    public static class DocumentationStorage
    {
        private static IDocumentationStorage s_Provider;

        /// <summary>The active storage provider. Initialized lazily with <see cref="MetaFileDocumentationStorage"/>.</summary>
        public static IDocumentationStorage Provider =>
            s_Provider ??= new MetaFileDocumentationStorage();

        /// <summary>Replaces the active storage provider.</summary>
        public static void SetProvider(IDocumentationStorage provider) =>
            s_Provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }
}
