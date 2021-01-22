#nullable enable
using System;
using Elffy;
using Elffy.OpenGL;
using Elffy.Shapes;
using Elffy.Serialization;
using Elffy.Shading;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Sandbox
{
    public static class Startup
    {
        public static async UniTask Start()
        {
            Definition.Initialize();

            await Definition.GenCameraMouse();

            await UniTask.WhenAll(
                Definition.GenAlicia(),
                Resources.Loader.CreateFbxModel("Dice.fbx").ActivateWaitLoaded());
            Debug.WriteLine("complete");
        }
    }
}
