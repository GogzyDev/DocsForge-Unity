using System;

namespace DocsForge.Markdown.InlineRenderers
{
    /// <summary>
    /// Marks a class as a renderer for one or more Markdig inline types.
    /// Discovered automatically by <c>MarkdownRenderer</c> via TypeCache.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InlineRendererAttribute : Attribute
    {
        /// <summary>The Markdig inline types this renderer handles.</summary>
        public Type[] InlineTypes { get; }

        /// <param name="inlineTypes">One or more <c>Markdig.Syntax.Inlines.Inline</c> subclasses this renderer handles.</param>
        public InlineRendererAttribute(params Type[] inlineTypes) => InlineTypes = inlineTypes;
    }
}
