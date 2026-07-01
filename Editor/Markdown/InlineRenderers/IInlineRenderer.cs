using System.Text;
using Markdig.Syntax.Inlines;

namespace DocsForge.Markdown.InlineRenderers
{
    /// <summary>Appends a Markdig inline's rich-text representation to a <see cref="StringBuilder"/>.</summary>
    public interface IInlineRenderer
    {
        /// <summary>Appends the rich-text representation of <paramref name="inline"/> to <paramref name="sb"/>.</summary>
        void Append(Inline inline, StringBuilder sb, IMarkdownRenderContext ctx);
    }

    /// <summary>
    /// Convenience base that handles the <see cref="Inline"/> → <typeparamref name="T"/> cast,
    /// so concrete renderers work with the strongly-typed inline directly.
    /// Use this for single-type renderers; implement <see cref="IInlineRenderer"/> directly for multi-type ones.
    /// </summary>
    public abstract class InlineRenderer<T> : IInlineRenderer where T : Inline
    {
        void IInlineRenderer.Append(Inline inline, StringBuilder sb, IMarkdownRenderContext ctx)
            => Append((T)inline, sb, ctx);

        /// <summary>Appends the rich-text representation of <paramref name="inline"/> to <paramref name="sb"/>.</summary>
        protected abstract void Append(T inline, StringBuilder sb, IMarkdownRenderContext ctx);
    }
}
