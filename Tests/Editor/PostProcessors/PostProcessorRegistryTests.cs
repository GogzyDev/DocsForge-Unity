using System.Linq;
using DocsForge.Core;
using DocsForge.PostProcessors;
using DocsForge.Settings;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace DocsForge.Tests.PostProcessors
{
    public class PostProcessorRegistryTests
    {
        [SetUp]
        public void SetUp() => PostProcessorRegistry.Reset();

        [TearDown]
        public void TearDown() => PostProcessorRegistry.Reset();

        [Test]
        public void EnsureDiscovered_FindsAttributedProductionProcessorsViaTypeCache()
        {
            var projectScoped = PostProcessorRegistry.GetPostProcessors(Scope.Project).Select(e => e.Processor).ToList();

            Assert.IsTrue(projectScoped.Any(p => p is PrefabDocumentationPostProcessor));
            Assert.IsTrue(projectScoped.Any(p => p is SoLayoutDocumentationPostProcessor));
            // TODO: when adding new built-in post processors, include them in the test as well.
        }

        [Test]
        public void GetAllApplicablePostProcessors_ProjectScoped_IncludesProcessor_WhenAppliesToReturnsTrue()
        {
            var fake = new FakeDocumentationPostProcessor(appliesTo: true);
            PostProcessorRegistry.Register(fake, "test.project-processor", Scope.Project);
            var doc = new AssetDocumentation();

            var applicable = PostProcessorRegistry.GetAllApplicablePostProcessors(doc, Scope.Project);

            Assert.IsTrue(applicable.Any(e => e.Id == "test.project-processor"));
        }

        [Test]
        public void GetAllApplicablePostProcessors_ProjectScoped_ExcludesProcessor_WhenDisabledViaProjectSettings()
        {
            const string id = "test.project-processor-disabled";
            var fake = new FakeDocumentationPostProcessor(appliesTo: true);
            PostProcessorRegistry.Register(fake, id, Scope.Project);
            var doc = new AssetDocumentation();

            var settings = DocsForgeProjectSettings.instance;
            var wasEnabled = settings.IsProcessorEnabled(id);
            try
            {
                settings.SetProcessorEnabled(id, false);

                var applicable = PostProcessorRegistry.GetAllApplicablePostProcessors(doc, Scope.Project);

                Assert.IsFalse(applicable.Any(e => e.Id == id));
            }
            finally
            {
                settings.SetProcessorEnabled(id, wasEnabled);
            }
        }

        [Test]
        public void GetAllApplicablePostProcessors_ProjectScoped_IncludesProcessor_WhenEnabledViaProjectSettings()
        {
            const string id = "test.project-processor-enabled";
            var fake = new FakeDocumentationPostProcessor(appliesTo: true);
            PostProcessorRegistry.Register(fake, id, Scope.Project);
            var doc = new AssetDocumentation();

            var settings = DocsForgeProjectSettings.instance;
            var wasEnabled = settings.IsProcessorEnabled(id);
            try
            {
                settings.SetProcessorEnabled(id, true);

                var applicable = PostProcessorRegistry.GetAllApplicablePostProcessors(doc, Scope.Project);

                Assert.IsTrue(applicable.Any(e => e.Id == id));
            }
            finally
            {
                settings.SetProcessorEnabled(id, wasEnabled);
            }
        }

        [Test]
        public void GetAppendices_AssetScoped_ExcludesProcessor_WhenIdAbsentFromEnabledList()
        {
            var fake = new FakeDocumentationPostProcessor(appliesTo: true, appendix: "asset-scoped-appendix");
            PostProcessorRegistry.Register(fake, "test.asset-processor-absent", Scope.Asset);
            var doc = new AssetDocumentation { EnabledAssetScopedProcessors = null };

            var appendices = PostProcessorRegistry.GetAppendices(doc).ToList();

            CollectionAssert.DoesNotContain(appendices, "asset-scoped-appendix");
        }

        [Test]
        public void GetAppendices_AssetScoped_IncludesProcessor_WhenIdPresentInEnabledList()
        {
            const string id = "test.asset-processor-present";
            var fake = new FakeDocumentationPostProcessor(appliesTo: true, appendix: "asset-scoped-appendix");
            PostProcessorRegistry.Register(fake, id, Scope.Asset);
            var doc = new AssetDocumentation { EnabledAssetScopedProcessors = new[] { id } };

            var appendices = PostProcessorRegistry.GetAppendices(doc).ToList();

            CollectionAssert.Contains(appendices, "asset-scoped-appendix");
        }

        private sealed class FakeDocumentationPostProcessor : IDocumentationPostProcessor
        {
            private readonly bool m_AppliesTo;
            private readonly string m_Appendix;

            public FakeDocumentationPostProcessor(bool appliesTo = true, string appendix = "fake-appendix")
            {
                m_AppliesTo = appliesTo;
                m_Appendix = appendix;
            }

            public bool AppliesTo(Object target) => m_AppliesTo;

            public string GenerateAppendix(Object target) => m_Appendix;
        }
    }
}
