using DocsForge.UriResolvers;
using NUnit.Framework;

namespace DocsForge.Tests.UriResolvers
{
    public class TypeUriResolverTests
    {
        [Test]
        public void TryResolve_FullyQualifiedTypeName_ProducesDocFxXref()
        {
            var resolver = new TypeUriResolver();

            var resolved = resolver.TryResolve("docsforge://type/System.String", out var output);

            Assert.IsTrue(resolved);
            Assert.AreEqual("@System.String", output);
        }
    }
}
