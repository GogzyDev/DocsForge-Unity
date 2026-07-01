using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    // Handles both indented code blocks and fenced code blocks (FencedCodeBlock : CodeBlock).
    [BlockRenderer(typeof(FencedCodeBlock), typeof(CodeBlock))]
    internal sealed class CodeBlockRenderer : IBlockRenderer
    {
        public void Render(Block block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var code = ((CodeBlock)block).Lines.ToString().TrimEnd();

            var wrapper = new VisualElement();
            wrapper.AddToClassList("md-code-block");

            var label = new Label(code);
            label.AddToClassList("md-code-block__content");
            label.enableRichText = false;

            wrapper.Add(label);
            parent.Add(wrapper);
        }
    }
}
