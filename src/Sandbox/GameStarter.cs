#nullable enable
using Elffy;
using Elffy.Games;
using Elffy.Mathematics;
using Elffy.Shading;
using Elffy.Shape;
using Elffy.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sandbox
{
    public static class GameStarter
    {
        public static void Start()
        {
            SingleScreenApp.MainCamera.LookAt(new Vector3(0, 10, 0), new Vector3(0, 10, 50));

            new CameraMouse(SingleScreenApp.MainCamera, SingleScreenApp.Mouse, Vector3.Zero).Activate();

            new Plain()
            {
                Scale = new Vector3(100),
                Rotation = new Quaternion(Vector3.UnitX, -90f.ToRadian()),
            }.Activate();

            PmxModel.LoadResourceAsync("Alicia/Alicia_solid.pmx").ContinueWithDispatch(model =>
            {
                model.Shader = ShaderSource.Normal;
                model.Scale = new Vector3(0.8f);
                model.Activate();
            });

            Model3D.LoadResourceAsync("green_frog.fbx").ContinueWithDispatch(model =>
            {
                model.Scale = new Vector3(0.03f);
                model.Position = new Vector3(10, 0, 0);
                model.Shader = ShaderSource.Normal;
                model.Activate();
            });

            new Cube()
            {
                Position = new Vector3(-5, 1, 0),
                Shader = ShaderSource.Normal,
            }.Activate();
        }
    }
}
