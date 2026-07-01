using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    [BlockRenderer(typeof(ParagraphBlock))]
    internal sealed class ParagraphBlockRenderer : BlockRenderer<ParagraphBlock>
    {
        protected override void Render(ParagraphBlock block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var label = new Label(ctx.BuildRichText(block.Inline));
            label.AddToClassList("md-paragraph");
            label.enableRichText = true;
            parent.Add(label);
        }
    }
}
