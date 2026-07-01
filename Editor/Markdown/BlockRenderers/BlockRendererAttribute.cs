using System;

namespace DocsForge.Markdown.BlockRenderers
{
    /// <summary>
    /// Marks a class as a renderer for one or more Markdig block types.
    /// Discovered automatically by <c>MarkdownRenderer</c> via TypeCache.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BlockRendererAttribute : Attribute
    {
        /// <summary>The Markdig block types this renderer handles.</summary>
        public Type[] BlockTypes { get; }

        /// <param name="blockTypes">One or more <c>Markdig.Syntax.Block</c> subclasses this renderer handles.</param>
        public BlockRendererAttribute(params Type[] blockTypes) => BlockTypes = blockTypes;
    }
}
