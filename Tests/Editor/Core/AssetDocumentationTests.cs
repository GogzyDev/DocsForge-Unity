using System;
using System.Collections.Generic;
using DocsForge.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DocsForge.Tests.Core
{
    public class AssetDocumentationTests
    {
        [Test]
        public void JsonRoundTrip_NestedSubObjects_ContentSurvives()
        {
            var leaf = new AssetDocumentation
            {
                Content = "Leaf content",
                EnabledAssetScopedProcessors = new[] { "docsforge.leaf-processor" }
            };

            var child = new AssetDocumentation
            {
                Content = "Child content",
                EnabledAssetScopedProcessors = new[] { "docsforge.child-processor" },
                SubObjects = new Dictionary<string, AssetDocumentation> { ["leaf-key"] = leaf }
            };

            var root = new AssetDocumentation
            {
                Content = "Root content",
                EnabledAssetScopedProcessors = new[] { "docsforge.root-processor" },
                SubObjects = new Dictionary<string, AssetDocumentation> { ["child-key"] = child }
            };

            var json = JsonConvert.SerializeObject(root);
            var deserialized = JsonConvert.DeserializeObject<AssetDocumentation>(json);

            Assert.AreEqual(root.Content, deserialized.Content);
            CollectionAssert.AreEqual(root.EnabledAssetScopedProcessors, deserialized.EnabledAssetScopedProcessors);

            var deserializedChild = deserialized.SubObjects["child-key"];
            Assert.AreEqual(child.Content, deserializedChild.Content);
            CollectionAssert.AreEqual(child.EnabledAssetScopedProcessors, deserializedChild.EnabledAssetScopedProcessors);

            var deserializedLeaf = deserializedChild.SubObjects["leaf-key"];
            Assert.AreEqual(leaf.Content, deserializedLeaf.Content);
            CollectionAssert.AreEqual(leaf.EnabledAssetScopedProcessors, deserializedLeaf.EnabledAssetScopedProcessors);
        }

        [Test]
        public void Serialize_ObjectIdIsAbsentFromOutput()
        {
            var go = new GameObject(nameof(Serialize_ObjectIdIsAbsentFromOutput));
            try
            {
                var doc = new AssetDocumentation { Content = "Some content" };
                doc.Initialize(GlobalObjectId.GetGlobalObjectIdSlow(go));

                var json = JObject.FromObject(doc);

                Assert.IsFalse(json.ContainsKey("ObjectId"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_StampsObjectId_OnRootEntry()
        {
            var go = new GameObject(nameof(Initialize_StampsObjectId_OnRootEntry));
            try
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(go);
                var doc = new AssetDocumentation();

                doc.Initialize(id);

                Assert.AreEqual(id, doc.ObjectId);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_RecursivelyStampsSubObjects_ParsedFromDictionaryKeys()
        {
            var rootGo = new GameObject(nameof(Initialize_RecursivelyStampsSubObjects_ParsedFromDictionaryKeys) + "_Root");
            var childGo = new GameObject(nameof(Initialize_RecursivelyStampsSubObjects_ParsedFromDictionaryKeys) + "_Child");
            try
            {
                var rootId = GlobalObjectId.GetGlobalObjectIdSlow(rootGo);
                var childId = GlobalObjectId.GetGlobalObjectIdSlow(childGo);

                var child = new AssetDocumentation();
                var root = new AssetDocumentation
                {
                    SubObjects = new Dictionary<string, AssetDocumentation>
                    {
                        [childId.ToString()] = child
                    }
                };

                root.Initialize(rootId);

                Assert.AreEqual(childId, child.ObjectId);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
                Object.DestroyImmediate(childGo);
            }
        }

        [Test]
        public void Initialize_CalledTwice_ThrowsInvalidOperationException()
        {
            var go = new GameObject(nameof(Initialize_CalledTwice_ThrowsInvalidOperationException));
            try
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(go);
                var doc = new AssetDocumentation();
                doc.Initialize(id);

                Assert.Throws<InvalidOperationException>(() => doc.Initialize(id));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
