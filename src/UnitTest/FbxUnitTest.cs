using System;
using System.IO;
using Elffy.Serialization;
using Xunit;

namespace UnitTest
{
    public class FbxUnitTest
    {
        [Fact]
        public void FbxModelBuildTest()
        {
            var files = Directory.GetFiles(TestValues.FileDirectory, "*.fbx");
            foreach(var path in files) {
                using var stream = File.OpenRead(path);
                ModelBuilder.BuildFromFbx(stream);
            }
        }
    }
}
