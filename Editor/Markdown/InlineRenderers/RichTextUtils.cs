namespace DocsForge.Markdown.InlineRenderers
{
    internal static class RichTextUtils
    {
        // Replaces '<' so Unity's rich-text parser sees it as a literal character, not a tag opener.
        internal static string Escape(string text) =>
            string.IsNullOrEmpty(text) ? text : text.Replace("<", "<noparse><</noparse>");
    }
}
