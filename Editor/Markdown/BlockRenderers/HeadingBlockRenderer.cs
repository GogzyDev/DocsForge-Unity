using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    [BlockRenderer(typeof(HeadingBlock))]
    internal sealed class HeadingBlockRenderer : BlockRenderer<HeadingBlock>
    {
        protected override void Render(HeadingBlock block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var label = new Label(ctx.BuildRichText(block.Inline));
            label.AddToClassList($"md-heading-{block.Level}");
            label.enableRichText = true;
            parent.Add(label);
        }
    }
}
