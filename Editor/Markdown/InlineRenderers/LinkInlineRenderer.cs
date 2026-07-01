using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(LinkInline))]
    internal sealed class LinkInlineRenderer : InlineRenderer<LinkInline>
    {
        protected override void Append(LinkInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
        {
            sb.Append("<color=#61AFEF>");
            foreach (var child in inline)
                ctx.AppendInline(child, sb);
            sb.Append("</color>");
        }
    }
}
