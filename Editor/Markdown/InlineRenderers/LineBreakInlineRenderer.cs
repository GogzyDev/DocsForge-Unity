using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(LineBreakInline))]
    internal sealed class LineBreakInlineRenderer : InlineRenderer<LineBreakInline>
    {
        protected override void Append(LineBreakInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
            => sb.Append(inline.IsHard ? "\n" : " ");
    }
}
