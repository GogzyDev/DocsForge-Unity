using DocsForge.Markdown;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DocsForge.Tests.Markdown
{
    public class MarkdownRendererTests
    {
        [Test]
        public void Render_NonEmptyMarkdown_ProducesNonEmptyContainer()
        {
            var container = MarkdownRenderer.Render("Hello world");

            Assert.IsNotNull(container);
            Assert.Greater(container.childCount, 0);
        }

        [Test]
        public void Render_BoldAndItalicRuns_ProduceRichTextTagsInParagraphLabel()
        {
            var container = MarkdownRenderer.Render("**bold** and *italic*");

            var paragraph = container.Q<Label>(className: "md-paragraph");

            Assert.IsNotNull(paragraph);
            StringAssert.Contains("<b>bold</b>", paragraph.text);
            StringAssert.Contains("<i>italic</i>", paragraph.text);
        }

        [Test]
        public void Render_UnorderedList_ProducesOneListItemElementPerItem()
        {
            var container = MarkdownRenderer.Render("- Item 1\n- Item 2\n- Item 3");

            var items = container.Query<VisualElement>(className: "md-list-item").ToList();

            Assert.AreEqual(3, items.Count);
        }
    }
}
