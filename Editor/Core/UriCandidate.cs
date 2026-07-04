using UnityEngine;

namespace DocsForge.Core
{
    /// <summary>A single entry surfaced in the in-editor link picker.</summary>
    public readonly struct UriCandidate
    {
        /// <summary>The full <c>docsforge://</c> URI that will be inserted into Markdown.</summary>
        public string Uri { get; }

        /// <summary>Human-readable label shown in the picker.</summary>
        public string DisplayName { get; }

        /// <summary>Injectable Markdown combined from <see cref="DisplayName"/> and <see cref="Uri"/></summary>
        public string Markdown => $"[{DisplayName}]({Uri})";

        /// <summary>Initializes a new <see cref="UriCandidate"/>.</summary>
        public UriCandidate(string uri, string displayName)
        {
            Uri = uri;
            DisplayName = displayName;
        }
    }
}
