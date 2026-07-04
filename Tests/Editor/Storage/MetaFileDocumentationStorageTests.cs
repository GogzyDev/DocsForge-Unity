using System.Linq;
using DocsForge.Core;
using DocsForge.Storage;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;

namespace DocsForge.Tests.Storage
{
    public class MetaFileDocumentationStorageTests
    {
        private const string k_DocsForgeLabel = "DocsForge";

        [SetUp]
        public void SetUp() => EditorTestAssets.EnsureCleanScratchFolder();

        [TearDown]
        public void TearDown() => EditorTestAssets.DeleteScratchFolder();

        [Test]
        public void Read_NoDocsforgeUserData_ReturnsNull()
        {
            var guid = EditorTestAssets.CreateAsset("NoUserData");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();

            var doc = storage.Read(guid);

            Assert.IsNull(doc);
        }

        [Test]
        public void Write_ThenRead_RoundTripsContent()
        {
            var guid = EditorTestAssets.CreateAsset("WriteRead");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            var data = new AssetDocumentation
            {
                Content = "Hello docs",
                EnabledAssetScopedProcessors = new[] { "docsforge.some-processor" }
            };

            storage.Write(guid, data);
            var result = storage.Read(guid);

            Assert.IsNotNull(result);
            Assert.AreEqual(data.Content, result.Content);
            CollectionAssert.AreEqual(data.EnabledAssetScopedProcessors, result.EnabledAssetScopedProcessors);
        }

        [Test]
        public void Write_DoesNotClobberExistingUserDataFromOtherTools()
        {
            var guid = EditorTestAssets.CreateAsset("PreserveOtherTool");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path);
            importer.userData = "{\"otherTool\":{\"value\":42}}";
            importer.SaveAndReimport();

            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            storage.Write(guid, new AssetDocumentation { Content = "Docs" });

            var envelope = JObject.Parse(AssetImporter.GetAtPath(path).userData);

            Assert.AreEqual(42, envelope["otherTool"]?["value"]?.Value<int>());
            Assert.IsTrue(envelope.ContainsKey("docsforge"));
        }

        [Test]
        public void Write_StampsDocsForgeLabelOnAsset()
        {
            var guid = EditorTestAssets.CreateAsset("StampLabel");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();

            storage.Write(guid, new AssetDocumentation { Content = "Docs" });

            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.Contains(AssetDatabase.GetLabels(asset), k_DocsForgeLabel);
        }

        [Test]
        public void Delete_RemovesDocsforgeKey_LeavesOtherUserDataIntact()
        {
            var guid = EditorTestAssets.CreateAsset("DeleteKeepOther");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path);
            importer.userData = "{\"otherTool\":{\"value\":7}}";
            importer.SaveAndReimport();

            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            storage.Write(guid, new AssetDocumentation { Content = "Docs" });
            storage.Delete(guid);

            var envelope = JObject.Parse(AssetImporter.GetAtPath(path).userData);

            Assert.IsFalse(envelope.ContainsKey("docsforge"));
            Assert.AreEqual(7, envelope["otherTool"]?["value"]?.Value<int>());
        }

        [Test]
        public void Delete_RemovesDocsForgeLabel()
        {
            var guid = EditorTestAssets.CreateAsset("DeleteLabel");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            storage.Write(guid, new AssetDocumentation { Content = "Docs" });

            storage.Delete(guid);

            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.DoesNotContain(AssetDatabase.GetLabels(asset), k_DocsForgeLabel);
        }

        [Test]
        public void Exists_ReturnsFalseBeforeWrite_TrueAfterWrite_FalseAfterDelete()
        {
            var guid = EditorTestAssets.CreateAsset("ExistsLifecycle");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();

            Assert.IsFalse(storage.Exists(guid));

            storage.Write(guid, new AssetDocumentation { Content = "Docs" });
            Assert.IsTrue(storage.Exists(guid));

            storage.Delete(guid);
            Assert.IsFalse(storage.Exists(guid));
        }

        [Test]
        public void FindAll_ReturnsOnlyGuidsWithDocsForgeLabel()
        {
            var documentedGuid = EditorTestAssets.CreateAsset("Documented");
            var undocumentedGuid = EditorTestAssets.CreateAsset("Undocumented");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();

            storage.Write(documentedGuid, new AssetDocumentation { Content = "Docs" });

            var found = storage.FindAll().ToList();

            CollectionAssert.Contains(found, documentedGuid);
            CollectionAssert.DoesNotContain(found, undocumentedGuid);
        }

        [Test]
        public void Write_SubObject_StoresDocInParentSubObjectsWithoutTouchingRootContent()
        {
            var (guid, _, child) = EditorTestAssets.CreatePrefabWithChild("WriteSubObject");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            var childId = GlobalObjectId.GetGlobalObjectIdSlow(child);

            storage.Write(guid, new AssetDocumentation { Content = "Root content" });
            storage.Write(childId, new AssetDocumentation { Content = "Child content" });

            var rootDoc = storage.Read(guid);

            Assert.AreEqual("Root content", rootDoc.Content);
            Assert.IsNotNull(rootDoc.SubObjects);
            Assert.AreEqual("Child content", rootDoc.SubObjects[childId.ToString()].Content);
        }

        [Test]
        public void Read_SubObject_ReturnsCorrectDocWithObjectIdStamped()
        {
            var (_, _, child) = EditorTestAssets.CreatePrefabWithChild("ReadSubObject");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            var childId = GlobalObjectId.GetGlobalObjectIdSlow(child);

            storage.Write(childId, new AssetDocumentation { Content = "Child content" });
            var childDoc = storage.Read(childId);

            Assert.IsNotNull(childDoc);
            Assert.AreEqual("Child content", childDoc.Content);
            Assert.AreEqual(childId, childDoc.ObjectId);
        }

        [Test]
        public void Exists_SubObject_ReturnsFalseBeforeWrite_TrueAfterWrite_FalseAfterDelete()
        {
            var (_, _, child) = EditorTestAssets.CreatePrefabWithChild("ExistsSubObject");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            var childId = GlobalObjectId.GetGlobalObjectIdSlow(child);

            Assert.IsFalse(storage.Exists(childId));

            storage.Write(childId, new AssetDocumentation { Content = "Child content" });
            Assert.IsTrue(storage.Exists(childId));

            storage.Delete(childId);
            Assert.IsFalse(storage.Exists(childId));
        }

        [Test]
        public void Delete_SubObject_RemovesOnlyThatEntry_RootLabelPreservedWhenRootHasContent()
        {
            var (guid, _, child) = EditorTestAssets.CreatePrefabWithChild("DeleteSubObjectKeepRoot");
            IDocumentationStorage storage = new MetaFileDocumentationStorage();
            var childId = GlobalObjectId.GetGlobalObjectIdSlow(child);

            storage.Write(guid, new AssetDocumentation { Content = "Root content" });
            storage.Write(childId, new AssetDocumentation { Content = "Child content" });

            storage.Delete(childId);

            var rootDoc = storage.Read(guid);
            Assert.AreEqual("Root content", rootDoc.Content);
            Assert.IsFalse(rootDoc.SubObjects != null && rootDoc.SubObjects.ContainsKey(childId.ToString()));

            var rootAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.Contains(AssetDatabase.GetLabels(rootAsset), k_DocsForgeLabel);
        }
    }
}
