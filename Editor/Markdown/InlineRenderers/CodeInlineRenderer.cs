using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    [InlineRenderer(typeof(CodeInline))]
    internal sealed class CodeInlineRenderer : InlineRenderer<CodeInline>
    {
        protected override void Append(CodeInline inline, StringBuilder sb, IMarkdownRenderContext ctx)
        {
            sb.Append("<color=#98C379>");
            sb.Append(RichTextUtils.Escape(inline.Content));
            sb.Append("</color>");
        }
    }
}
