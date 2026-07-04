using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Lets the user browse and pick any type known to <see cref="ProjectTypeCache"/>, grouped by namespace.
    /// </summary>
    internal sealed class TypePickerDropdown : AdvancedDropdown
    {
        private readonly Action<Type> m_OnTypePicked;

        public TypePickerDropdown(Action<Type> onTypePicked) : base(new AdvancedDropdownState())
        {
            m_OnTypePicked = onTypePicked;
            minimumSize = new Vector2(300, 350);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            var namespaceItems = new Dictionary<string, AdvancedDropdownItem>();

            foreach (var type in ProjectTypeCache.GetTypes())
            {
                var ns = string.IsNullOrEmpty(type.Namespace) ? "(no namespace)" : type.Namespace;
                if (!namespaceItems.TryGetValue(ns, out var namespaceItem))
                {
                    namespaceItem = new AdvancedDropdownItem(ns);
                    namespaceItems.Add(ns, namespaceItem);
                    root.AddChild(namespaceItem);
                }

                namespaceItem.AddChild(new TypeDropdownItem(type));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeItem)
                m_OnTypePicked(typeItem.Type);
        }

        private sealed class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }

            public TypeDropdownItem(Type type) : base(type.Name)
            {
                Type = type;
            }
        }
    }
}
