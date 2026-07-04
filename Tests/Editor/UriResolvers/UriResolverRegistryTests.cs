using System;
using DocsForge.Core;
using DocsForge.UriResolvers;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace DocsForge.Tests.UriResolvers
{
    public class UriResolverRegistryTests
    {
        [SetUp]
        public void SetUp() => UriResolverRegistry.Reset();

        [TearDown]
        public void TearDown() => UriResolverRegistry.Reset();

        [Test]
        public void EnsureDiscovered_FindsAttributedProductionResolversViaTypeCache()
        {
            var foundAsset = UriResolverRegistry.TryGetResolverByType<AssetUriResolver>(out var assetResolver);
            var foundType = UriResolverRegistry.TryGetResolverByPrefix(TypeUriResolver.k_Prefix, out var typeResolver);
            // TODO: in case new built-in resolvers are added, its worth adding them to the test.

            Assert.IsTrue(foundAsset);
            Assert.IsNotNull(assetResolver);
            Assert.IsTrue(foundType);
            Assert.IsInstanceOf<TypeUriResolver>(typeResolver);
        }

        [Test]
        public void TryResolve_DispatchesToResolverMatchingLongestPrefix()
        {
            var shortScope = new FakeUriResolver("short-match");
            var nestedScope = new FakeUriResolver("long-match");
            UriResolverRegistry.Register(shortScope, "test://scope/");
            UriResolverRegistry.Register(nestedScope, "test://scope/nested/");

            Assert.IsTrue(UriResolverRegistry.TryResolve("test://scope/nested/thing", out var nestedOutput));
            Assert.AreEqual("long-match", nestedOutput);

            Assert.IsTrue(UriResolverRegistry.TryResolve("test://scope/other", out var shortOutput));
            Assert.AreEqual("short-match", shortOutput);
        }

        [Test]
        public void TryResolve_ReturnsFalse_ForUnregisteredPrefix()
        {
            var resolved = UriResolverRegistry.TryResolve("docsforge://totally-unknown/abc", out var output);

            Assert.IsFalse(resolved);
            Assert.IsNull(output);
        }

        [Test]
        public void Register_ManualResolver_IsFindableAndDispatchable()
        {
            var fake = new FakeUriResolver("manual-output");

            UriResolverRegistry.Register(fake, "test://manual/", "Manual Test");

            Assert.IsTrue(UriResolverRegistry.TryGetResolverByPrefix("test://manual/", out var found));
            Assert.AreSame(fake, found);

            Assert.IsTrue(UriResolverRegistry.TryResolve("test://manual/anything", out var output));
            Assert.AreEqual("manual-output", output);
        }

        private sealed class FakeUriResolver : IUriResolver
        {
            private readonly string m_ResolvedOutput;

            public FakeUriResolver(string resolvedOutput) => m_ResolvedOutput = resolvedOutput;

            public void OpenPicker(VisualElement anchor, Action<UriCandidate> onSelected) { }

            public bool TryResolve(string uri, out string output)
            {
                output = m_ResolvedOutput;
                return true;
            }

            public bool TryOpenInEditor(string uri) => false;

            public bool TryMakeUri(object target, out UriCandidate uriCandidate)
            {
                uriCandidate = default;
                return false;
            }
        }
    }
}
