using DocsForge.PostProcessors;
using UnityEngine;

namespace DocsForge.Core
{
    /// <summary>
    /// Generates appendix for an asset documentation page.
    /// Register via <see cref="DocumentationPostProcessorAttribute"/> or <see cref="PostProcessorRegistry.Register"/>.
    /// </summary>
    public interface IDocumentationPostProcessor
    {
        /// <summary>Returns true if this processor should run for the <paramref name="target"/> asset.</summary>
        bool AppliesTo(Object target);

        /// <summary>Generate Markdown appendix for <paramref name="target"/> asset documentation page.</summary>
        string GenerateAppendix(Object target);
    }
}
