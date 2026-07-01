using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DocsForge.Core
{
    /// <summary>Documentation data for a single asset or sub-object.</summary>
    public class AssetDocumentation
    {
        /// <summary>Markdown content authored by the user.</summary>
        public string Content;

        /// <summary>IDs of asset-scoped post-processors the user has opted into for this asset.</summary>
        public string[] EnabledAssetScopedProcessors;

        /// <summary>
        /// Documentation for sub-objects (scene objects, prefab children, nested assets),
        /// keyed by <c>GlobalObjectId</c> string.
        /// </summary>
        public Dictionary<string, AssetDocumentation> SubObjects;

        /// <summary>
        /// The Unity object identity of this asset or sub-object. Set once via <see cref="Initialize"/>
        /// after deserialization; never written to storage.
        /// </summary>
        public GlobalObjectId ObjectId { get; private set; }

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private Object m_CachedTarget;

        public Object Target => m_CachedTarget ??= GlobalObjectId.GlobalObjectIdentifierToObjectSlow(ObjectId);

        /// <summary>
        /// Stamps this instance with its <see cref="GlobalObjectId"/> and recursively initializes
        /// all <see cref="SubObjects"/>. May only be called once; throws <see cref="InvalidOperationException"/>
        /// on subsequent calls.
        /// </summary>
        public void Initialize(GlobalObjectId id)
        {
            if (m_Initialized)
                throw new InvalidOperationException("AssetDocumentation has already been initialized.");

            m_Initialized = true;
            ObjectId = id;

            if (SubObjects == null)
                return;

            foreach (var (key, subDoc) in SubObjects)
            {
                if (GlobalObjectId.TryParse(key, out var subId))
                    subDoc.Initialize(subId);
            }
        }
    }
}
