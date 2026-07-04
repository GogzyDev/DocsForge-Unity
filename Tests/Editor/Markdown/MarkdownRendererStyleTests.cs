using System.Collections;
using DocsForge.Markdown;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DocsForge.Tests.Markdown
{
    public class MarkdownRendererStyleTests
    {
        private MarkdownTestHostWindow m_Window;

        [SetUp]
        public void SetUp()
        {
            m_Window = ScriptableObject.CreateInstance<MarkdownTestHostWindow>();
            m_Window.ShowUtility();
            m_Window.position = new Rect(-10000, -10000, 400, 400);
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        [UnityTest]
        public IEnumerator HeadingFontSizes_StrictlyDecrease_AcrossConsecutiveLevels()
        {
            var root = MarkdownRenderer.Render("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6");
            m_Window.rootVisualElement.Add(root);

            yield return null;
            yield return null;

            var sizes = new float[6];
            for (var level = 1; level <= 6; level++)
            {
                var label = root.Q<Label>(className: $"md-heading-{level}");
                Assert.IsNotNull(label, $"Missing heading label for level {level}");
                sizes[level - 1] = label.resolvedStyle.fontSize;
            }

            for (var i = 0; i < sizes.Length - 1; i++)
            {
                Assert.Greater(sizes[i], sizes[i + 1],
                    $"Expected md-heading-{i + 1} font size ({sizes[i]}) to be greater than md-heading-{i + 2} ({sizes[i + 1]})");
            }
        }

        [UnityTest]
        public IEnumerator CodeBlockFontFamily_DiffersFromParagraphFontFamily()
        {
            var root = MarkdownRenderer.Render("Some paragraph text.\n\n```\ncode line\n```");
            m_Window.rootVisualElement.Add(root);

            yield return null;
            yield return null;

            var paragraph = root.Q<Label>(className: "md-paragraph");
            var code = root.Q<Label>(className: "md-code-block__content");

            Assert.IsNotNull(paragraph);
            Assert.IsNotNull(code);

            var paragraphFont = paragraph.resolvedStyle.unityFontDefinition;
            var codeFont = code.resolvedStyle.unityFontDefinition;

            Assert.AreNotEqual(paragraphFont, codeFont);
        }
    }
}
