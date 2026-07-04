namespace DocsForge.Core
{
    /// <summary>On what level post processors usage is defined.</summary>
    public enum Scope
    {
        /// <summary>User opts in on per-asset level.</summary>
        Asset,
        /// <summary>User opts out on project level (globally).</summary>
        Project,
    }

    /// <summary>Placement of documentation overview popup.</summary>
    public enum PopupOpenStyle
    {
        /// <summary>Popup opens under inspectors header.</summary>
        Under,
        /// <summary>Popup opens next to inspector window.</summary>
        OnTheSide,
    }

    /// <summary>What action triggers opening documentation overview popup.</summary>
    public enum PopupOpenMethod
    {
        /// <summary>Popup opens automatically after hovering over the inspector header row.</summary>
        Hover,
        /// <summary>A dedicated button next to "Edit" opens the popup as a focused window that closes on focus loss.</summary>
        Button,
    }
}