#nullable enable
using Elffy;
using Elffy.Components;
using Elffy.Games;
using Elffy.Imaging;
using Elffy.Mathematics;
using Elffy.Shading;
using Elffy.Shape;
using Elffy.Threading;
using Elffy.UI;

namespace Sandbox
{
    public static class GameStarter
    {
        public static void Start()
        {
            SingleScreenApp.MainCamera.LookAt(new Vector3(0, 3, 0), new Vector3(0, 4.5f, 20));

            new CameraMouse(SingleScreenApp.MainCamera, SingleScreenApp.Mouse, Vector3.Zero).Activate();

            var plain = new Plain()
            {
                Scale = new Vector3(20),
                Rotation = new Quaternion(Vector3.UnitX, -90f.ToRadian()),
            };
            plain.AddComponent(new Material(new Color4(0.8f), new Color4(0.15f), new Color4(0.2f), 400f));
            plain.AddComponent(Resources.GetStream("cube.png").ToTexture(BitmapType.Png));
            plain.Activate();

            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                model.Scale = new Vector3(0.3f);
                model.AddComponent(new Material(new Color4(0.8f), new Color4(0.25f), new Color4(0.1f), 5f));
                model.Activate();
            });

            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                model.Position = new Vector3(-5, 0, -5);
                model.Scale = new Vector3(0.3f);
                model.Shader = ShaderSource.Normal;
                model.Activate();
            });

            Model3D.LoadResourceAsync("green_frog.fbx").ContinueWithDispatch(model =>
            {
                model.Scale = new Vector3(0.01f);
                model.Position = new Vector3(5, 0, 0);
                model.AddComponent(new Material(new Color4(0f, 0.7f, 0.25f), new Color4(0f, 0.6f, 0.1f), Color4.White, 4));
                model.Activate();
            });

            new Cube()
            {
                Position = new Vector3(-3, 0.5f, 0),
                Shader = ShaderSource.Normal,
            }.Activate();

            var cube = new Cube()
            {
                Position = new Vector3(-3, 0.5f, -3),
            };
            var cubeTexture = Resources.GetStream("cube.png").ToTexture(BitmapType.Png);
            cube.AddComponent(cubeTexture);
            cube.Activate();

            SingleScreenApp.UI.Add(new Button(90, 30));
        }
    }
}
