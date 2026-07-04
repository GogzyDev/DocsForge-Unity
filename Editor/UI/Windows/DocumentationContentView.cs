using DocsForge.Core;
using DocsForge.Markdown;
using DocsForge.PostProcessors;
using UnityEngine.UIElements;

namespace DocsForge.UI.Windows
{
    /// <summary>
    /// Renders an <see cref="AssetDocumentation"/>'s Markdown content followed by its enabled
    /// post-processor appendices into a <see cref="VisualElement"/>. Shared by every window that
    /// displays the full documentation body (preview tab, hover popup).
    /// </summary>
    internal static class DocumentationContentView
    {
        /// <summary>Appends the rendered documentation body to <paramref name="container"/>. Does not clear it first.</summary>
        public static void Render(VisualElement container, AssetDocumentation documentation)
        {
            container.Add(MarkdownRenderer.Render(documentation.Content));

            foreach (var appendix in PostProcessorRegistry.GetAppendices(documentation))
                container.Add(MarkdownRenderer.Render(appendix));
        }
    }
}
