using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Elffy.Serialization;

namespace Test
{
    [TestClass]
    public class FbxUnitTest
    {
        private readonly string FileDirectory = Path.Combine("..", "..", "testfile");

        [TestMethod]
        public void FbxModelBuildTest()
        {
            var files = Directory.GetFiles(FileDirectory, "*.fbx");
            foreach(var path in files) {
                using(var stream = File.Open(path, FileMode.Open)) {
                    var model = FbxModelBuilder.LoadModel(stream);
                }
            }
        }
    }
}
