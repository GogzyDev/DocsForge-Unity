using System;
using System.Collections.Generic;
using System.Linq;
using DocsForge.Core;
using UnityEditor;

namespace DocsForge.UriResolvers
{
    /// <summary>
    /// Central registry for all <see cref="IUriResolver"/> implementations.
    /// Automatically discovers types decorated with <see cref="UriResolverAttribute"/> via TypeCache
    /// on first access. Resolvers requiring constructor arguments can be registered manually via
    /// <see cref="Register"/>; manually registered resolvers take precedence over attribute-discovered
    /// ones with the same prefix.
    /// </summary>
    public static class UriResolverRegistry
    {
        private readonly struct Entry
        {
            public readonly IUriResolver Resolver;
            public readonly string Prefix;
            public readonly string DisplayName;

            public Entry(IUriResolver resolver, string prefix, string displayName)
            {
                Resolver = resolver;
                Prefix = prefix;
                DisplayName = displayName;
            }
        }

        private static readonly List<Entry> s_Entries = new();
        private static bool s_Discovered;

        /// <summary>
        /// Manually registers a resolver. Takes precedence over any attribute-discovered resolver
        /// with the same prefix. Safe to call before or after auto-discovery runs.
        /// </summary>
        /// <param name="resolver">The resolver instance to register.</param>
        /// <param name="prefix">The URI prefix this resolver handles, e.g. <c>docsforge://asset/</c>.</param>
        /// <param name="displayName">Human-readable label shown in the Insert Link menu. Null for headless resolvers.</param>
        public static void Register(IUriResolver resolver, string prefix, string displayName = null)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

            RemoveByPrefix(prefix);
            s_Entries.Add(new Entry(resolver, prefix, displayName));
        }

        /// <summary>
        /// Attempts to resolve <paramref name="uri"/> to an export-time Markdown fragment using the
        /// registered resolver whose prefix best matches the URI.
        /// See <see cref="IUriResolver.TryResolve"/> for the three-way return convention
        /// (link target / plain-text fallback / unhandled).
        /// </summary>
        public static bool TryResolve(string uri, out string output)
        {
            EnsureDiscovered();
            var entry = FindByPrefix(uri);
            if (entry != null)
                return entry.Value.Resolver.TryResolve(uri, out output);

            output = null;
            return false;
        }

        /// <summary>
        /// Handles a URI click in the in-editor Markdown preview using the matching registered resolver.
        /// Returns false if no resolver handles the URI or the resolver does not support in-editor navigation.
        /// </summary>
        public static bool TryOpenInEditor(string uri)
        {
            EnsureDiscovered();
            var entry = FindByPrefix(uri);
            return entry != null && entry.Value.Resolver.TryOpenInEditor(uri);
        }

        /// <summary>
        /// Returns all resolvers that have a non-null display name, in registration order.
        /// Used to build the Insert Link top-level menu.
        /// </summary>
        public static IReadOnlyList<(string DisplayName, IUriResolver Resolver)> GetUiResolvers()
        {
            EnsureDiscovered();
            return s_Entries
                .Where(e => e.DisplayName != null)
                .Select(e => (e.DisplayName, e.Resolver))
                .ToList();
        }

        /// <summary>
        /// Returns the resolver registered under <paramref name="prefix"/> using an exact match.
        /// </summary>
        public static bool TryGetResolverByPrefix(string prefix, out IUriResolver resolver)
        {
            EnsureDiscovered();
            foreach (var entry in s_Entries)
            {
                if (entry.Prefix == prefix)
                {
                    resolver = entry.Resolver;
                    return true;
                }
            }
            resolver = null;
            return false;
        }

        /// <summary>
        /// Returns the resolver whose <see cref="Entry.DisplayName"/> equals <paramref name="name"/>.
        /// Headless resolvers (null display name) are never matched.
        /// </summary>
        public static bool TryGetResolverByName(string name, out IUriResolver resolver)
        {
            EnsureDiscovered();
            foreach (var entry in s_Entries)
            {
                if (entry.DisplayName == name)
                {
                    resolver = entry.Resolver;
                    return true;
                }
            }
            resolver = null;
            return false;
        }

        /// <summary>
        /// Returns the resolver whose runtime type exactly matches <paramref name="type"/>.
        /// </summary>
        public static bool TryGetResolverByType(Type type, out IUriResolver resolver)
        {
            EnsureDiscovered();
            foreach (var entry in s_Entries)
            {
                if (entry.Resolver.GetType() == type)
                {
                    resolver = entry.Resolver;
                    return true;
                }
            }
            resolver = null;
            return false;
        }

        /// <summary>
        /// Returns the resolver whose runtime type exactly matches <typeparamref name="T"/>.
        /// </summary>
        public static bool TryGetResolverByType<T>(out T resolver) where T : IUriResolver
        {
            if (TryGetResolverByType(typeof(T), out var r))
            {
                resolver = (T)r;
                return true;
            }
            resolver = default;
            return false;
        }

        /// <summary>Clears all entries and resets auto-discovery. For test isolation only.</summary>
        internal static void Reset()
        {
            s_Entries.Clear();
            s_Discovered = false;
        }

        private static void EnsureDiscovered()
        {
            if (s_Discovered)
                return;

            s_Discovered = true;

            foreach (var type in TypeCache.GetTypesWithAttribute<UriResolverAttribute>())
            {
                if (!typeof(IUriResolver).IsAssignableFrom(type))
                    continue;

                var attrs = type.GetCustomAttributes(typeof(UriResolverAttribute), false);
                if (attrs.Length == 0)
                    continue;

                var attr = (UriResolverAttribute)attrs[0];

                // Manual registrations take precedence; skip auto-discovered types whose prefix
                // is already covered by a manually registered resolver.
                if (s_Entries.Any(e => e.Prefix == attr.Prefix))
                    continue;

                IUriResolver resolver;
                try
                {
                    resolver = (IUriResolver)Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                s_Entries.Add(new Entry(resolver, attr.Prefix, attr.DisplayName));
            }
        }

        private static void RemoveByPrefix(string prefix)
        {
            for (var i = s_Entries.Count - 1; i >= 0; i--)
            {
                if (s_Entries[i].Prefix == prefix)
                    s_Entries.RemoveAt(i);
            }
        }

        // Longest-prefix match so more specific prefixes win over shorter overlapping ones.
        private static Entry? FindByPrefix(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            Entry? best = null;
            foreach (var entry in s_Entries)
            {
                if (!uri.StartsWith(entry.Prefix, StringComparison.Ordinal))
                    continue;

                if (best == null || entry.Prefix.Length > best.Value.Prefix.Length)
                    best = entry;
            }

            return best;
        }
    }
}
