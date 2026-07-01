using System;
using System.Collections.Generic;
using System.Text;
using DocsForge.Markdown.BlockRenderers;
using DocsForge.Markdown.InlineRenderers;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEditor;
using UnityEngine.UIElements;

namespace DocsForge.Markdown
{
    /// <summary>
    /// Parses a Markdown string via Markdig and produces a UI Toolkit <see cref="VisualElement"/> tree.
    /// Block and inline rendering is fully extensible: annotate a class with
    /// <see cref="BlockRendererAttribute"/> or <see cref="InlineRendererAttribute"/> and it is
    /// discovered automatically via TypeCache — no changes to this class required.
    /// </summary>
    public static class MarkdownRenderer
    {
        private const string k_StyleSheetPath =
            "Packages/com.gogzydev.docsforge/Editor/Markdown/MarkdownRenderer.uss";

        private static readonly MarkdownPipeline s_Pipeline =
            new MarkdownPipelineBuilder().Build();

        private static Dictionary<Type, IBlockRenderer> s_BlockRenderers;
        private static Dictionary<Type, IInlineRenderer> s_InlineRenderers;

        /// <summary>
        /// Renders <paramref name="markdown"/> into a <see cref="VisualElement"/> tree.
        /// Passing null or whitespace-only input produces a placeholder element.
        /// </summary>
        public static VisualElement Render(string markdown)
        {
            EnsureDiscovered();

            var container = new VisualElement();
            container.AddToClassList("md-container");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath);
            if (styleSheet != null)
                container.styleSheets.Add(styleSheet);

            if (string.IsNullOrWhiteSpace(markdown))
            {
                var empty = new Label("(no content)");
                empty.AddToClassList("md-empty");
                container.Add(empty);
                return container;
            }

            var document = Markdig.Markdown.Parse(markdown, s_Pipeline);
            var ctx = new RenderContext();

            foreach (var block in document)
                ctx.RenderBlock(block, container);

            return container;
        }

        private static void EnsureDiscovered()
        {
            if (s_BlockRenderers != null)
                return;

            s_BlockRenderers = new Dictionary<Type, IBlockRenderer>();
            s_InlineRenderers = new Dictionary<Type, IInlineRenderer>();

            foreach (var type in TypeCache.GetTypesWithAttribute<BlockRendererAttribute>())
            {
                if (!typeof(IBlockRenderer).IsAssignableFrom(type))
                    continue;

                var attrs = type.GetCustomAttributes(typeof(BlockRendererAttribute), false);
                if (attrs.Length == 0)
                    continue;

                IBlockRenderer renderer;
                try { renderer = (IBlockRenderer)Activator.CreateInstance(type); }
                catch { continue; }

                foreach (var blockType in ((BlockRendererAttribute)attrs[0]).BlockTypes)
                    s_BlockRenderers[blockType] = renderer;
            }

            foreach (var type in TypeCache.GetTypesWithAttribute<InlineRendererAttribute>())
            {
                if (!typeof(IInlineRenderer).IsAssignableFrom(type))
                    continue;

                var attrs = type.GetCustomAttributes(typeof(InlineRendererAttribute), false);
                if (attrs.Length == 0)
                    continue;

                IInlineRenderer renderer;
                try { renderer = (IInlineRenderer)Activator.CreateInstance(type); }
                catch { continue; }

                foreach (var inlineType in ((InlineRendererAttribute)attrs[0]).InlineTypes)
                    s_InlineRenderers[inlineType] = renderer;
            }
        }

        private sealed class RenderContext : IMarkdownRenderContext
        {
            public void RenderBlock(Block block, VisualElement parent)
            {
                if (s_BlockRenderers.TryGetValue(block.GetType(), out var renderer))
                {
                    renderer.Render(block, parent, this);
                    return;
                }

                // Unregistered container blocks: recurse into children so structure is preserved.
                if (block is ContainerBlock container)
                {
                    foreach (var child in container)
                        RenderBlock(child, parent);
                }
            }

            public void AppendInline(Inline inline, StringBuilder sb)
            {
                if (s_InlineRenderers.TryGetValue(inline.GetType(), out var renderer))
                {
                    renderer.Append(inline, sb, this);
                    return;
                }

                // Unregistered container inlines: recurse into children.
                if (inline is ContainerInline container)
                {
                    foreach (var child in container)
                        AppendInline(child, sb);
                }
            }

            public string BuildRichText(ContainerInline inlines)
            {
                if (inlines == null)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var inline in inlines)
                    AppendInline(inline, sb);

                return sb.ToString();
            }
        }
    }
}
