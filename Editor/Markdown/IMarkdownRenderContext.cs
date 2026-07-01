using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEngine.UIElements;

namespace DocsForge.Markdown
{
    /// <summary>
    /// Passed into every block and inline renderer to allow recursive dispatch
    /// without coupling individual renderers to the registry or to each other.
    /// </summary>
    public interface IMarkdownRenderContext
    {
        /// <summary>Dispatches <paramref name="block"/> to the registered renderer, falling back to child iteration for unregistered container blocks.</summary>
        void RenderBlock(Block block, VisualElement parent);

        /// <summary>Dispatches <paramref name="inline"/> to the registered renderer, falling back to child iteration for unregistered container inlines.</summary>
        void AppendInline(Inline inline, StringBuilder sb);

        /// <summary>Iterates <paramref name="inlines"/> via <see cref="AppendInline"/> and returns the accumulated rich-text string.</summary>
        string BuildRichText(ContainerInline inlines);
    }
}
