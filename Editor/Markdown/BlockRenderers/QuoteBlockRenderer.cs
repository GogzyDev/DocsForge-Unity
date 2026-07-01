using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    [BlockRenderer(typeof(QuoteBlock))]
    internal sealed class QuoteBlockRenderer : BlockRenderer<QuoteBlock>
    {
        protected override void Render(QuoteBlock block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var wrapper = new VisualElement();
            wrapper.AddToClassList("md-blockquote");

            foreach (var child in block)
                ctx.RenderBlock(child, wrapper);

            parent.Add(wrapper);
        }
    }
}
