using System;
using System.IO;
using System.Linq;
using Elffy;
using Elffy.Serialization;
using Xunit;

namespace UnitTest
{
    public class FbxUnitTest
    {
        [Fact]
        public void FbxModelBuildTest()
        {
            var loader = new LocalFileResourceLoader(TestValues.FileDirectory);
            var files = Directory.GetFiles(TestValues.FileDirectory, "*.fbx")
                                 .Select(path => Path.GetFileName(path));
            foreach(var file in files) {
                using var stream = File.OpenRead(Path.Combine(loader.CurrentDirectory, file));
                _ = ModelBuilder.BuildFromFbx(loader, file);
            }
        }
    }

    internal sealed class LocalFileResourceLoader : IResourceLoader
    {
        public string CurrentDirectory { get; }

        public LocalFileResourceLoader(string currentDir)
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

        public bool HasResource(string name) => throw new NotSupportedException();
    }
}
