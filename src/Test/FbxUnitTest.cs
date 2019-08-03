using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Elffy.Serialization;

namespace Test
{
    [TestClass]
    public class FbxUnitTest
    {
        [TestMethod]
        public void FbxModelBuildTest()
        {
            var files = Directory.GetFiles(TestValues.FileDirectory, "*.fbx");
            foreach(var path in files) {
                using(var stream = File.Open(path, FileMode.Open)) {
                    var model = FbxModelBuilder.LoadModel(stream);
                }
            }
        }
    }
}
