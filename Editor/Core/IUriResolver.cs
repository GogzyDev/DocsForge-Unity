using System;
using UnityEngine.UIElements;

namespace DocsForge.Core
{
    /// <summary>
    /// Resolves <c>docsforge://</c> URIs to export-time Markdown fragments, handles in-editor
    /// navigation when a link is clicked, and drives the in-editor link picker.
    /// Register via <see cref="UriResolverAttribute"/> or <c>UriResolverRegistry.Register</c>.
    /// </summary>
    public interface IUriResolver
    {
        /// <summary>
        /// Opens a picker UI anchored to <paramref name="anchor"/> so the window appears in a
        /// sensible position regardless of how the action was triggered (mouse or keyboard).
        /// Invoke <paramref name="onSelected"/> with the chosen candidate when the user confirms.
        /// The registry only calls this for resolvers whose <see cref="UriResolverAttribute.DisplayName"/> is non-null.
        /// </summary>
        void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected);

        /// <summary>
        /// Attempts to resolve a URI to an export-time Markdown fragment.
        /// </summary>
        /// <param name="uri">The <c>docsforge://</c> URI to resolve.</param>
        /// <param name="output">
        /// When returning <c>true</c>: the resolved link target (e.g. a relative <c>.md</c> path or a DocFX xref).
        /// When returning <c>false</c> with a non-null value: the URI is recognized but has no page;
        /// the export pipeline should emit <paramref name="output"/> as plain text instead of a link.
        /// When returning <c>false</c> with a null value: the URI is not handled by this resolver.
        /// </param>
        bool TryResolve(string uri, out string output);

        /// <summary>
        /// Handles a URI click in the in-editor Markdown preview (e.g. ping an asset in the
        /// Project window, or open a script in the IDE).
        /// Returns false if this resolver does not support in-editor navigation for the given URI.
        /// </summary>
        bool TryOpenInEditor(string uri);

        /// <summary>
        /// Attempts to create URI for a target object.
        /// </summary>
        /// <param name="target">Object URI should point to</param>
        /// <param name="uriCandidate">Constructed URI representation</param>
        bool TryMakeUri(object target, out UriCandidate uriCandidate);
    }
}
