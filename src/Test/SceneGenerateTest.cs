using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SceneGenerator;
using System.IO;

namespace Test
{
    [TestClass]
    public class SceneGenerateTest
    {
        [TestMethod]
        public void SceneCodeGenerate()
        {
            var sceneDir = new DirectoryInfo(Path.Combine(TestValues.FileDirectory, "TestScene"));
            var outputDir = new DirectoryInfo("SceneOutput");
            CodeGenerator.GenerateAll(sceneDir, outputDir);
        }
    }
}
