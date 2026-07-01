using Markdig.Syntax;
using UnityEngine.UIElements;

namespace DocsForge.Markdown.BlockRenderers
{
    /// <summary>Renders a Markdig block into a <see cref="VisualElement"/> subtree.</summary>
    public interface IBlockRenderer
    {
        /// <summary>Appends the visual representation of <paramref name="block"/> into <paramref name="parent"/>.</summary>
        void Render(Block block, VisualElement parent, IMarkdownRenderContext ctx);
    }

    /// <summary>
    /// Convenience base that handles the <see cref="Block"/> → <typeparamref name="T"/> cast,
    /// so concrete renderers work with the strongly-typed block directly.
    /// Use this for single-type renderers; implement <see cref="IBlockRenderer"/> directly for multi-type ones.
    /// </summary>
    public abstract class BlockRenderer<T> : IBlockRenderer where T : Block
    {
        void IBlockRenderer.Render(Block block, VisualElement parent, IMarkdownRenderContext ctx)
            => Render((T)block, parent, ctx);

        /// <summary>Appends the visual representation of <paramref name="block"/> into <paramref name="parent"/>.</summary>
        protected abstract void Render(T block, VisualElement parent, IMarkdownRenderContext ctx);
    }
}
