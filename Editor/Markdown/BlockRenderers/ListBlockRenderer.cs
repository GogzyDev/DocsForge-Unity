using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    [BlockRenderer(typeof(ListBlock))]
    internal sealed class ListBlockRenderer : BlockRenderer<ListBlock>
    {
        protected override void Render(ListBlock block, VisualElement parent, IMarkdownRenderContext ctx)
        {
            var listEl = new VisualElement();
            listEl.AddToClassList("md-list");

            var index = block.OrderedStart is { } start && int.TryParse(start, out var parsed) ? parsed : 1;

            foreach (var item in block)
            {
                if (item is ListItemBlock listItem)
                    RenderItem(listItem, listEl, index++, block.IsOrdered, ctx);
            }

            parent.Add(listEl);
        }

        private static void RenderItem(ListItemBlock item, VisualElement parent, int index, bool ordered, IMarkdownRenderContext ctx)
        {
            var row = new VisualElement();
            row.AddToClassList("md-list-item");

            var bullet = new Label(ordered ? $"{index}." : "•");
            bullet.AddToClassList("md-list-item__bullet");
            row.Add(bullet);

            var content = new VisualElement();
            content.AddToClassList("md-list-item__content");

            // Tight list item: single paragraph — render inline to avoid paragraph margin.
            if (item.Count == 1 && item[0] is ParagraphBlock para)
            {
                var text = new Label(ctx.BuildRichText(para.Inline));
                text.AddToClassList("md-list-item__text");
                text.enableRichText = true;
                content.Add(text);
            }
            else
            {
                foreach (var child in item)
                    ctx.RenderBlock(child, content);
            }

            row.Add(content);
            parent.Add(row);
        }
    }
}
