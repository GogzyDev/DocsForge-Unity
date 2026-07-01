using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(HtmlEntityInline))]
    internal sealed class HtmlEntityInlineRenderer : InlineRenderer<HtmlEntityInline>
    {
        protected override void Append(HtmlEntityInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
            => sb.Append(RichTextUtils.Escape(inline.Transcoded.ToString()));
    }
}
