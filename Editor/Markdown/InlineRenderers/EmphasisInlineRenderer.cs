using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(EmphasisInline))]
    internal sealed class EmphasisInlineRenderer : InlineRenderer<EmphasisInline>
    {
        protected override void Append(EmphasisInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
        {
            var tag = inline.DelimiterCount >= 2 ? "b" : "i";
            sb.Append($"<{tag}>");
            foreach (var child in inline)
                ctx.AppendInline(child, sb);
            sb.Append($"</{tag}>");
        }
    }
}
