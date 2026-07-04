using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(LinkInline))]
    internal sealed class LinkInlineRenderer : InlineRenderer<LinkInline>
    {
        protected override void Append(LinkInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
        {
            var hasUrl = !string.IsNullOrEmpty(inline.Url);
            if (hasUrl)
                sb.Append("<link=\"").Append(inline.Url).Append("\">");

            sb.Append("<color=#61AFEF><u>");
            foreach (var child in inline)
                ctx.AppendInline(child, sb);
            sb.Append("</u></color>");

            if (hasUrl)
                sb.Append("</link>");
        }
    }
}
