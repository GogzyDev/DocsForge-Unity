using DocsForge.Core;
using NUnit.Framework;

namespace DocsForge.Tests.Core
{
    public class DocumentationPostProcessorAttributeTests
    {
        [Test]
        public void Ctor_Defaults_ScopeIsProjectAndDisplayNameIsNull()
        {
            var attribute = new DocumentationPostProcessorAttribute("docsforge.my-processor");

            Assert.AreEqual("docsforge.my-processor", attribute.Id);
            Assert.AreEqual(Scope.Project, attribute.Scope);
            Assert.IsNull(attribute.DisplayName);
        }

        [Test]
        public void Ctor_ExplicitValues_ExposesIdScopeAndDisplayName()
        {
            var attribute = new DocumentationPostProcessorAttribute("docsforge.my-processor", Scope.Asset, "My Processor");

            Assert.AreEqual("docsforge.my-processor", attribute.Id);
            Assert.AreEqual(Scope.Asset, attribute.Scope);
            Assert.AreEqual("My Processor", attribute.DisplayName);
        }
    }
}
