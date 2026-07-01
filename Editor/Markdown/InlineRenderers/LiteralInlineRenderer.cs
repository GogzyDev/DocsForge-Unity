using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(LiteralInline))]
    internal sealed class LiteralInlineRenderer : InlineRenderer<LiteralInline>
    {
        protected override void Append(LiteralInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
            => sb.Append(RichTextUtils.Escape(inline.Content.ToString()));
    }
}
