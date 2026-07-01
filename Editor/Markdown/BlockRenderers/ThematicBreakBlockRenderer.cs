using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    [BlockRenderer(typeof(ThematicBreakBlock))]
    internal sealed class ThematicBreakBlockRenderer : BlockRenderer<ThematicBreakBlock>
    {
        protected override void Render(ThematicBreakBlock block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var hr = new VisualElement();
            hr.AddToClassList("md-hr");
            parent.Add(hr);
        }
    }
}
