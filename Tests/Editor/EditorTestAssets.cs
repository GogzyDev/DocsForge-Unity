using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DocsForge.Tests
{
    /// <summary>
    /// Shared helpers for tests that need real, disk-backed Unity assets — required whenever the
    /// code under test goes through <c>AssetImporter</c>/<c>AssetDatabase</c>, neither of which can
    /// be faked. Assets are created under <see cref="ScratchFolder"/>; callers are expected to wipe
    /// it via <see cref="DeleteScratchFolder"/> in their own <c>[TearDown]</c>.
    /// </summary>
    internal static class EditorTestAssets
    {
        public const string ScratchFolder = "Assets/DocsForgeTests_Temp";

        public static void EnsureCleanScratchFolder()
        {
            AssetDatabase.DeleteAsset(ScratchFolder);
            AssetDatabase.CreateFolder("Assets", "DocsForgeTests_Temp");
        }

        public static void DeleteScratchFolder()
        {
            AssetDatabase.DeleteAsset(ScratchFolder);
        }

        public static string CreateAsset(string name)
        {
            var asset = ScriptableObject.CreateInstance<DummyTestAsset>();
            var path = $"{ScratchFolder}/{name}.asset";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            return AssetDatabase.AssetPathToGUID(path);
        }

        // Creates a real prefab with a child GameObject so sub-object tests exercise a genuine
        // GlobalObjectId that shares the parent asset's GUID but has a distinct local file ID,
        // instead of hand-fabricating an ID string.
        public static (string guid, GameObject root, GameObject child) CreatePrefabWithChild(string name)
        {
            var rootGo = new GameObject("Root");
            var childGo = new GameObject("Child");
            childGo.transform.SetParent(rootGo.transform);

            var path = $"{ScratchFolder}/{name}.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootGo, path);
            Object.DestroyImmediate(rootGo);

            var guid = AssetDatabase.AssetPathToGUID(path);
            var childInAsset = prefabAsset.transform.Find("Child").gameObject;

            return (guid, prefabAsset, childInAsset);
        }
    }
}
