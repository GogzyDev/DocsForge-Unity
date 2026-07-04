using UnityEditor;

namespace DocsForge.Tests.Markdown
{
    // Minimal, off-screen EditorWindow used only to give rendered VisualElement trees a real panel
    // to attach to, so UI Toolkit can run its layout/style resolution pass during tests.
    internal class MarkdownTestHostWindow : EditorWindow
    {
    }
}
