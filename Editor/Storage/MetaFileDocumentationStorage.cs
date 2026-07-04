using System.Collections.Generic;
using System.Linq;
using DocsForge.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DocsForge.Storage
{
    /// <summary>
    /// Stores documentation in <c>AssetImporter.userData</c>, persisted inside Unity's <c>.meta</c> files.
    /// Treats <c>userData</c> as a shared JSON envelope and reads/writes only the <c>"docsforge"</c> key,
    /// leaving any other tools' data untouched.
    /// </summary>
    public class MetaFileDocumentationStorage : IDocumentationStorage
    {
        private const string k_UserDataKey = "docsforge";
        private const string k_AssetLabel = "DocsForge";

        private static readonly JsonSerializer k_Serializer = JsonSerializer.CreateDefault();

        /// <inheritdoc/>
        public AssetDocumentation Read(string guid)
        {
            var importer = GetImporter(guid, out var path);
            if (importer == null)
                return null;

            var token = ParseEnvelope(importer.userData)[k_UserDataKey];
            if (token == null)
                return null;

            var doc = token.ToObject<AssetDocumentation>(k_Serializer);
            if (doc == null)
                return null;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null)
                doc.Initialize(GlobalObjectId.GetGlobalObjectIdSlow(mainAsset));

            return doc;
        }

        /// <inheritdoc/>
        public void Write(string guid, AssetDocumentation data)
        {
            var importer = GetImporter(guid, out var path);
            if (importer == null)
                return;

            var envelope = ParseEnvelope(importer.userData);
            envelope[k_UserDataKey] = JObject.FromObject(data, k_Serializer);
            importer.userData = envelope.ToString(Formatting.None);
            importer.SaveAndReimport();

            SetLabel(AssetDatabase.LoadMainAssetAtPath(path), add: true);
        }

        /// <inheritdoc/>
        public void Delete(string guid)
        {
            var importer = GetImporter(guid, out var path);
            if (importer == null)
                return;

            var envelope = ParseEnvelope(importer.userData);
            if (!envelope.Remove(k_UserDataKey))
                return;

            importer.userData = envelope.HasValues
                ? envelope.ToString(Formatting.None)
                : string.Empty;
            importer.SaveAndReimport();

            SetLabel(AssetDatabase.LoadMainAssetAtPath(path), add: false);
        }

        /// <inheritdoc/>
        public bool Exists(string guid)
        {
            var importer = GetImporter(guid, out _);
            return importer != null && ParseEnvelope(importer.userData).ContainsKey(k_UserDataKey);
        }

        /// <inheritdoc/>
        public IEnumerable<string> FindAll() =>
            AssetDatabase.FindAssets($"l:{k_AssetLabel}");

        /// <inheritdoc/>
        public AssetDocumentation Read(GlobalObjectId id)
        {
            var assetGuid = id.assetGUID.ToString();

            if (IsMainAsset(id, assetGuid))
                return Read(assetGuid);

            var importer = GetImporter(assetGuid, out var path);
            if (importer == null)
                return null;

            var token = ParseEnvelope(importer.userData)[k_UserDataKey];
            if (token == null)
                return null;

            var rootDoc = token.ToObject<AssetDocumentation>(k_Serializer);
            if (rootDoc == null)
                return null;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null)
                rootDoc.Initialize(GlobalObjectId.GetGlobalObjectIdSlow(mainAsset));

            AssetDocumentation subDoc = null;
            rootDoc.SubObjects?.TryGetValue(id.ToString(), out subDoc);
            return subDoc;
        }

        /// <inheritdoc/>
        public void Write(GlobalObjectId id, AssetDocumentation data)
        {
            var assetGuid = id.assetGUID.ToString();

            if (IsMainAsset(id, assetGuid))
            {
                Write(assetGuid, data);
                return;
            }

            var importer = GetImporter(assetGuid, out var path);
            if (importer == null)
                return;

            var envelope = ParseEnvelope(importer.userData);
            var rootDoc = envelope[k_UserDataKey]?.ToObject<AssetDocumentation>(k_Serializer)
                          ?? new AssetDocumentation();
            rootDoc.SubObjects ??= new Dictionary<string, AssetDocumentation>();
            rootDoc.SubObjects[id.ToString()] = data;

            envelope[k_UserDataKey] = JObject.FromObject(rootDoc, k_Serializer);
            importer.userData = envelope.ToString(Formatting.None);
            importer.SaveAndReimport();

            SetLabel(AssetDatabase.LoadMainAssetAtPath(path), add: true);
        }

        /// <inheritdoc/>
        public void Delete(GlobalObjectId id)
        {
            var assetGuid = id.assetGUID.ToString();

            if (IsMainAsset(id, assetGuid))
            {
                Delete(assetGuid);
                return;
            }

            var importer = GetImporter(assetGuid, out _);
            if (importer == null)
                return;

            var rootDoc = ReadRaw(importer);
            if (rootDoc?.SubObjects == null || !rootDoc.SubObjects.Remove(id.ToString()))
                return;

            var envelope = ParseEnvelope(importer.userData);
            envelope[k_UserDataKey] = JObject.FromObject(rootDoc, k_Serializer);
            importer.userData = envelope.ToString(Formatting.None);
            importer.SaveAndReimport();
        }

        /// <inheritdoc/>
        public bool Exists(GlobalObjectId id)
        {
            var assetGuid = id.assetGUID.ToString();

            if (IsMainAsset(id, assetGuid))
                return Exists(assetGuid);

            var importer = GetImporter(assetGuid, out _);
            return importer != null
                   && (ReadRaw(importer)?.SubObjects?.ContainsKey(id.ToString()) == true);
        }

        // Deserializes the root AssetDocumentation without calling Initialize.
        // Used when we need to inspect or mutate SubObjects without stamping ObjectId.
        private static AssetDocumentation ReadRaw(AssetImporter importer)
        {
            var token = ParseEnvelope(importer.userData)[k_UserDataKey];
            return token?.ToObject<AssetDocumentation>(k_Serializer);
        }

        // Returns true when id refers to the main (root) asset of the file, not a sub-object.
        private static bool IsMainAsset(GlobalObjectId id, string assetGuid)
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(path))
                return true;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset == null)
                return true;

            return id.Equals(GlobalObjectId.GetGlobalObjectIdSlow(mainAsset));
        }

        private static AssetImporter GetImporter(string guid, out string path)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetImporter.GetAtPath(path);
        }

        // If userData contains non-JSON content written by another tool, we return an empty
        // envelope rather than failing. This means we cannot read that asset's userData, but
        // we also cannot safely merge into it, so we treat it as a fresh start.
        private static JObject ParseEnvelope(string userData)
        {
            if (string.IsNullOrEmpty(userData))
                return new JObject();

            try
            {
                return JObject.Parse(userData);
            }
            catch (JsonException)
            {
                return new JObject();
            }
        }

        private static void SetLabel(Object asset, bool add)
        {
            if (asset == null)
                return;

            var labels = AssetDatabase.GetLabels(asset).ToList();
            var hasLabel = labels.Contains(k_AssetLabel);

            if (add && !hasLabel)
                labels.Add(k_AssetLabel);
            else if (!add && hasLabel)
                labels.Remove(k_AssetLabel);
            else
                return;

            AssetDatabase.SetLabels(asset, labels.ToArray());
        }
    }
}
