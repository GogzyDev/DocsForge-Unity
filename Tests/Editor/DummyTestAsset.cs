using UnityEngine;

namespace DocsForge.Tests
{
    // Minimal ScriptableObject used purely as a disk-backed fixture asset: production code that
    // goes through AssetImporter/AssetDatabase requires a real asset that lives on disk.
    internal class DummyTestAsset : ScriptableObject
    {
    }
}
