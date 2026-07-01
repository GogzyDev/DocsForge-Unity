using System.Collections.Generic;
using UnityEditor;

namespace DocsForge.Core
{
    /// <summary>
    /// Abstraction over the storage backend for asset documentation.
    /// Operations are keyed by asset GUID for root assets, or by <see cref="GlobalObjectId"/>
    /// when working with sub-objects (scene objects, prefab children, nested assets).
    /// </summary>
    public interface IDocumentationStorage
    {
        /// <summary>Reads documentation for the asset with the given GUID, or <c>null</c> if none exists.</summary>
        AssetDocumentation Read(string guid);

        /// <summary>
        /// Reads documentation for the object identified by <paramref name="id"/>.
        /// For root assets this is equivalent to <see cref="Read(string)"/> with the asset GUID.
        /// For sub-objects, returns the matching <see cref="AssetDocumentation.SubObjects"/> entry.
        /// </summary>
        AssetDocumentation Read(GlobalObjectId id);

        /// <summary>Writes documentation for the asset with the given GUID.</summary>
        void Write(string guid, AssetDocumentation data);

        /// <summary>
        /// Writes documentation for the object identified by <paramref name="id"/>.
        /// For sub-objects, updates the matching entry inside the parent asset's
        /// <see cref="AssetDocumentation.SubObjects"/> without touching the rest of the root document.
        /// </summary>
        void Write(GlobalObjectId id, AssetDocumentation data);

        /// <summary>Deletes all documentation for the asset with the given GUID.</summary>
        void Delete(string guid);

        /// <summary>
        /// Deletes documentation for the object identified by <paramref name="id"/>.
        /// For sub-objects, removes only that entry from <see cref="AssetDocumentation.SubObjects"/>.
        /// </summary>
        void Delete(GlobalObjectId id);

        /// <summary>Returns true if documentation exists for the asset with the given GUID.</summary>
        bool Exists(string guid);

        /// <summary>
        /// Returns true if documentation exists for the object identified by <paramref name="id"/>.
        /// For sub-objects, checks <see cref="AssetDocumentation.SubObjects"/> on the parent asset.
        /// </summary>
        bool Exists(GlobalObjectId id);

        /// <summary>Returns the GUIDs of all assets that have documentation.</summary>
        IEnumerable<string> FindAll();
    }
}
