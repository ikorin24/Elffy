#nullable enable
using System.IO;
using System.Linq;
using Elffy;
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

        public string Name => "Test";

        public TestFileResourceLoader(string currentDir)
        {
            CurrentDirectory = currentDir;
        }

        public bool TryGetStream(string? name, out Stream stream)
        {
            if(name is null) {
                stream = Stream.Null;
                return false;
            }
            stream = File.OpenRead(Path.Combine(CurrentDirectory, name));
            return true;
        }

        public bool TryGetSize(string? name, out long size)
        {
            if(name is null) {
                size = 0;
                return false;
            }
            size = new FileInfo(Path.Combine(CurrentDirectory, name)).Length;
            return true;
        }

        public bool Exists(string? name) => true;

        public bool TryGetHandle(string? name, out ResourceFileHandle handle)
        {
            if(name is null) {
                handle = ResourceFileHandle.None;
                return false;
            }
            var path = Path.Combine(CurrentDirectory, name);
            var size = new FileInfo(path).Length;
            handle = new ResourceFileHandle(File.OpenHandle(path), 0, size);
            return true;
        }
    }
}
