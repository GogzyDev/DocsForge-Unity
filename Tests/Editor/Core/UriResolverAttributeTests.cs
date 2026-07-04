using DocsForge.Core;
using NUnit.Framework;

namespace DocsForge.Tests.Core
{
    public class UriResolverAttributeTests
    {
        [Test]
        public void Ctor_HeadlessOverload_SetsPrefixOnlyAndDisplayNameIsNull()
        {
            var attribute = new UriResolverAttribute("docsforge://asset/");

            Assert.AreEqual("docsforge://asset/", attribute.Prefix);
            Assert.IsNull(attribute.DisplayName);
        }

        [Test]
        public void Ctor_WithDisplayName_SetsBothPrefixAndDisplayName()
        {
            var attribute = new UriResolverAttribute("docsforge://asset/", "Asset");

            Assert.AreEqual("docsforge://asset/", attribute.Prefix);
            Assert.AreEqual("Asset", attribute.DisplayName);
        }
    }
}
