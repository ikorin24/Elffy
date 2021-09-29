#nullable enable
using System.IO;
using System.Linq;
using Elffy;
using Elffy.Core;
using Elffy.Serialization.Fbx;
using Xunit;

namespace UnitTest
{
    public class FbxUnitTest
    {
        [Fact]
        public void CreateFbxModelTest()
        {
            var loader = new TestFileResourceLoader(TestValues.FileDirectory);
            var files = Directory.GetFiles(TestValues.FileDirectory, "*.fbx")
                                 .Select(path => new ResourceFile(loader, Path.GetFileName(path)));

            foreach(var file in files) {
                var model = FbxModelBuilder.CreateLazyLoadingFbx(file);
                Assert.True(model is not null);
            }
        }

        [Fact]
        public void LoadFbxModelTest()
        {
            var loader = new TestFileResourceLoader(TestValues.FileDirectory);
            var files = Directory.GetFiles(TestValues.FileDirectory, "*.fbx")
                                 .Select(path => new ResourceFile(loader, Path.GetFileName(path)));

            foreach(var file in files) {
                using var stream = file.GetStream();
                using var fbx = FbxSemanticParser<SkinnedVertex>.Parse(stream);
                Assert.True(fbx is not null);
                Assert.True(fbx!.Vertices.IsEmpty == false);
                Assert.True(fbx.Indices.IsEmpty == false);
            }

            foreach(var file in files) {
                using var stream = file.GetStream();
                using var fbx = FbxSemanticParser<SkinnedVertex>.ParseUnsafe(stream);
                Assert.True(fbx.Vertices.IsEmpty == false);
                Assert.True(fbx.Indices.IsEmpty == false);
            }
        }
    }

    internal sealed class TestFileResourceLoader : IResourceLoader
    {
        public string CurrentDirectory { get; }

        public TestFileResourceLoader(string currentDir)
        {
            CurrentDirectory = currentDir;
        }

        public long GetSize(string name)
        {
            return new FileInfo(Path.Combine(CurrentDirectory, name)).Length;
        }

        public Stream GetStream(string name)
        {
            return File.OpenRead(Path.Combine(CurrentDirectory, name));
        }

        public bool HasResource(string name) => true;
    }
}
