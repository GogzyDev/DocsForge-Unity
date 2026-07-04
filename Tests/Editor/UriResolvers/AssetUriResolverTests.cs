using DocsForge.Core;
using DocsForge.Storage;
using DocsForge.UriResolvers;
using NUnit.Framework;
using UnityEditor;

namespace DocsForge.Tests.UriResolvers
{
    public class AssetUriResolverTests
    {
        [SetUp]
        public void SetUp() => EditorTestAssets.EnsureCleanScratchFolder();

        [TearDown]
        public void TearDown() => EditorTestAssets.DeleteScratchFolder();

        [Test]
        public void TryResolve_DocumentedRootAsset_ProducesRelativeMdPath()
        {
            var guid = EditorTestAssets.CreateAsset("DocumentedRoot");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);
            DocumentationStorage.Provider.Write(guid, new AssetDocumentation { Content = "Docs" });

            var resolver = new AssetUriResolver();
            var resolved = resolver.TryResolve($"docsforge://asset/{id}", out var output);

            Assert.IsTrue(resolved);
            Assert.AreEqual(path + ".md", output);
        }

        [Test]
        public void TryResolve_UndocumentedRootAsset_FallsBackToPlainPath()
        {
            var guid = EditorTestAssets.CreateAsset("UndocumentedRoot");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var id = GlobalObjectId.GetGlobalObjectIdSlow(asset);

            var resolver = new AssetUriResolver();
            var resolved = resolver.TryResolve($"docsforge://asset/{id}", out var output);

            Assert.IsFalse(resolved);
            Assert.AreEqual(path, output);
        }

        [Test]
        public void TryResolve_DocumentedSubObjectSharingFile_ProducesRelativeMdPathUnderParent()
        {
            var (guid, _, child) = EditorTestAssets.CreatePrefabWithChild("DocumentedSubObject");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var id = GlobalObjectId.GetGlobalObjectIdSlow(child);
            DocumentationStorage.Provider.Write(id, new AssetDocumentation { Content = "Child docs" });

            var resolver = new AssetUriResolver();
            var resolved = resolver.TryResolve($"docsforge://asset/{id}", out var output);

            Assert.IsTrue(resolved);
            Assert.AreEqual($"{path}/{child.name}.md", output);
        }
    }
}
