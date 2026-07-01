using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DocsForge.Core;
using DocsForge.UriResolvers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DocsForge.PostProcessors
{
    /// <summary>
    /// Appends the serialized field layout of a ScriptableObject to its documentation page,
    /// including field name, declared type, and <see cref="TooltipAttribute"/> content when present.
    /// </summary>
    [DocumentationPostProcessor("docsforge.so-layout", scope: Scope.Project, displayName: "Serialized Properties")]
    public class SoLayoutDocumentationPostProcessor : IDocumentationPostProcessor
    {
        private const int k_MaxDepth = 3;
        private const int k_MaxCollectionItemsInline = 8;

        private static readonly string[] ExcludedNamespacePrefixes =
        {
            "UnityEngine",
            "UnityEditor",
            "Unity.",
        };
        
        /// <inheritdoc/>
        public bool AppliesTo(Object target)
        {
            if (target is not ScriptableObject so)
                return false;
            
            if (MonoScript.FromScriptableObject(so) == null)
                return false;

            var ns = target.GetType().Namespace ?? string.Empty;
            if (ExcludedNamespacePrefixes.Any(ns.StartsWith))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public string GenerateAppendix(Object target)
        {
            if (target == null)
                return "*(null object)*";

            var so = new SerializedObject(target);
            var sb = new StringBuilder();

            sb.AppendLine("| Field | Type | Description | Value |");
            sb.AppendLine("|---|---|---|---|");

            SerializedProperty prop = so.GetIterator();
            var enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.propertyPath == "m_Script")
                    continue;

                AppendRow(sb, prop, target.GetType(), 0);
            }

            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, SerializedProperty prop, Type ownerType, int depth)
        {
            var fieldInfo = GetFieldViaPath(ownerType, prop.propertyPath);
            var fieldType = fieldInfo?.FieldType;

            var fieldName = prop.displayName;
            var typeName = GetFriendlyTypeName(fieldType, prop);
            var description = string.IsNullOrEmpty(prop.tooltip) ? "" : Escape(prop.tooltip);
            var value = FormatValue(prop, fieldType, depth);

            sb.AppendLine($"| {Escape(fieldName)} | `{typeName}` | {description} | {value} |");
        }

        #region Value Formatting
        private static string FormatValue(SerializedProperty prop, Type fieldType, int depth)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return prop.boolValue ? "✅ true" : "❌ false";
                case SerializedPropertyType.Float:
                    return prop.floatValue.ToString("0.###");
                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(prop.stringValue) ? "*(empty)*" : $"\"{Escape(prop.stringValue)}\"";
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    var hex = ColorUtility.ToHtmlStringRGBA(c);
                    return $"`#{hex}` (R:{c.r:0.00} G:{c.g:0.00} B:{c.b:0.00} A:{c.a:0.00})";
                case SerializedPropertyType.ObjectReference:
                    return FormatObjectReference(prop.objectReferenceValue);
                case SerializedPropertyType.Enum:
                    if (prop.enumValueIndex < 0 || prop.enumValueIndex >= prop.enumDisplayNames.Length)
                        return "*(invalid)*";
                    return $"`{prop.enumDisplayNames[prop.enumValueIndex]}`";
                case SerializedPropertyType.Vector2:
                    return $"({prop.vector2Value.x:0.##}, {prop.vector2Value.y:0.##})";
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return $"({v3.x:0.##}, {v3.y:0.##}, {v3.z:0.##})";
                case SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return $"({v4.x:0.##}, {v4.y:0.##}, {v4.z:0.##}, {v4.w:0.##})";
                case SerializedPropertyType.Rect:
                    var r = prop.rectValue;
                    return $"(x:{r.x:0.##}, y:{r.y:0.##}, w:{r.width:0.##}, h:{r.height:0.##})";
                case SerializedPropertyType.AnimationCurve:
                    return $"*(curve, {prop.animationCurveValue.length} keys)*";
                case SerializedPropertyType.ManagedReference:
                    return FormatManagedReference(prop, depth);
                case SerializedPropertyType.Generic:
                    if (prop.isArray)
                        return FormatCollection(prop, fieldType, depth);
                    return FormatNestedObject(prop, fieldType, depth);
                default:
                    return Escape(prop.ToString());
            }
        }

        private static string FormatObjectReference(Object obj)
        {
            if (obj == null)
                return "*(none)*";

            if (UriResolverRegistry.TryGetResolverByType<AssetUriResolver>(out var uriResolver) &&
                uriResolver.TryMakeUri(obj, out var uri))
            {
                return uri.Markdown;
            }
            
            if (EditorUtility.IsPersistent(obj))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                return $"`{path}`";
            }

            return $"*(scene instance)* `{obj.name}`";
        }

        private static string FormatCollection(SerializedProperty prop, Type fieldType, int depth)
        {
            var count = prop.arraySize;
            if (count == 0)
                return "*(empty, 0 items)*";

            var elementType = GetCollectionElementType(fieldType);
            var isSimple = IsSimpleType(elementType);

            var items = new List<string>();
            var shown = Mathf.Min(count, k_MaxCollectionItemsInline);

            for (int i = 0; i < shown; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                var formatted = isSimple
                    ? FormatValue(element, elementType, depth + 1)
                    : $"[{i}] {FormatValue(element, elementType, depth + 1)}";
                items.Add(formatted);
            }

            var joiner = isSimple ? ", " : "<br>";
            var body = string.Join(joiner, items);

            if (count > shown)
                body += $"{joiner}*(+{count - shown} more)*";

            return $"**{count} item(s):** {(isSimple ? body : "<br>" + body)}";
        }

        private static string FormatNestedObject(SerializedProperty prop, Type fieldType, int depth)
        {
            if (depth >= k_MaxDepth)
                return "(...)";

            var fields = new List<string>();
            var child = prop.Copy();
            var end = prop.GetEndProperty();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                var fi = GetFieldViaPath(fieldType, GetLocalPath(child, prop));
                var val = FormatValue(child, fi?.FieldType, depth + 1);
                fields.Add($"**{child.displayName}:** {val}");
            }

            return fields.Count == 0 ? "(...)" : string.Join("<br>", fields);
        }

        private static string FormatManagedReference(SerializedProperty prop, int depth)
        {
            if (string.IsNullOrEmpty(prop.managedReferenceFullTypename))
                return "*(null)*";

            var parts = prop.managedReferenceFullTypename.Split(' ');
            var typeName = parts.Length > 1 ? parts[1].Split('.').Last() : prop.managedReferenceFullTypename;

            if (depth >= k_MaxDepth)
                return $"**[{typeName}]** (...)";

            var actualType = Type.GetType(prop.managedReferenceFullTypename.Replace(" ", ", "));
            var nested = FormatNestedObject(prop, actualType, depth);

            return $"**[{typeName}]**<br>{nested}";
        }
        #endregion

        #region Type Name Formatting
        private static string GetFriendlyTypeName(Type type, SerializedProperty prop)
        {
            if (type == null)
            {
                return prop.propertyType == SerializedPropertyType.ManagedReference
                    ? "SerializeReference"
                    : prop.type;
            }

            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";

            if (type.IsArray)
                return $"{GetFriendlyTypeName(type.GetElementType(), prop)}[]";

            if (type.IsGenericType)
            {
                string genericName = type.GetGenericTypeDefinition().Name.Split('`')[0];
                string args = string.Join(", ", type.GetGenericArguments().Select(t => GetFriendlyTypeName(t, prop)));
                return $"{genericName}<{args}>";
            }

            return type.Name;
        }

        private static bool IsSimpleType(Type type)
        {
            if (type == null) return true;
            return type.IsPrimitive || type == typeof(string) || type == typeof(Color)
                || type.IsEnum || typeof(Object).IsAssignableFrom(type);
        }

        private static Type GetCollectionElementType(Type fieldType)
        {
            if (fieldType == null) return null;
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType) return fieldType.GetGenericArguments()[0];
            return null;
        }
        #endregion

        #region Reflections
        private static FieldInfo GetFieldViaPath(Type rootType, string path)
        {
            if (rootType == null) return null;

            FieldInfo fieldInfo = null;
            var currentType = rootType;
            var parts = path.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
                {
                    currentType = GetCollectionElementType(currentType);
                    if (currentType == null) return null;
                    i++;
                    continue;
                }

                fieldInfo = currentType.GetField(parts[i],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (fieldInfo == null) return null;
                currentType = fieldInfo.FieldType;
            }

            return fieldInfo;
        }

        private static string GetLocalPath(SerializedProperty child, SerializedProperty parent)
        {
            var full = child.propertyPath;
            var prefix = parent.propertyPath + ".";
            return full.StartsWith(prefix) ? full[prefix.Length..] : full;
        }
        #endregion

        private static string Escape(string s)
        {
            return s?.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "") ?? "";
        }
    }
}
