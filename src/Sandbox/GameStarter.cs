#nullable enable
using Elffy;
using Elffy.Components;
using Elffy.Games;
using Elffy.Mathematics;
using Elffy.Shading;
using Elffy.Shape;
using Elffy.Threading;

namespace Sandbox
{
    public static class GameStarter
    {
        public static void Start()
        {
            SingleScreenApp.MainCamera.LookAt(new Vector3(0, 10, 0), new Vector3(0, 10, 50));

            new CameraMouse(SingleScreenApp.MainCamera, SingleScreenApp.Mouse, Vector3.Zero).Activate();

            var plain = new Plain()
            {
                Scale = new Vector3(100),
                Rotation = new Quaternion(Vector3.UnitX, -90f.ToRadian()),
            };
            plain.AddComponent(new Material(new Color4(0.8f), new Color4(0.15f), new Color4(0.2f), 400f));
            plain.Activate();

            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                model.Scale = new Vector3(0.8f);
                model.AddComponent(new Material(new Color4(0.8f), new Color4(0.25f), new Color4(0.1f), 5f));
                model.Activate();
            });

            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                model.Position = new Vector3(-10, 0, -10);
                model.Scale = new Vector3(0.8f);
                model.Shader = ShaderSource.Normal;
                model.Activate();
            });

            Model3D.LoadResourceAsync("green_frog.fbx").ContinueWithDispatch(model =>
            {
                model.Scale = new Vector3(0.03f);
                model.Position = new Vector3(10, 0, 0);
                model.AddComponent(new Material(new Color4(0f, 0.7f, 0.25f), new Color4(0f, 0.6f, 0.1f), Color4.White, 4));
                model.Activate();
            });

            new Cube()
            {
                Position = new Vector3(-5, 1, 0),
                Shader = ShaderSource.Normal,
            }.Activate();

            new Cube()
            {
                Position = new Vector3(-5, 1, -5),
            }.Activate();
        }
    }
}
