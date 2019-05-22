using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public abstract class Positionable : FrameObject
    {
        protected Matrix4 _modelView = Matrix4.Identity;

        public Vector3 Position
        {
            get { return _modelView.Row3.Xyz; }
            set { _modelView.Row3.Xyz = value; }
        }

        public float ScaleX
        {
            get { return _modelView.Row0.X; }
            set { _modelView.Row0.X = value; }
        }

        public float ScaleY
        {
            get { return _modelView.Row1.Y; }
            set { _modelView.Row1.Y = value; }
        }

        public float ScaleZ
        {
            get { return _modelView.Row2.Z; }
            set { _modelView.Row2.Z = value; }
        }

        public void Translate(float x, float y, float z)
        {
            if(x == 0 && y == 0 && z == 0) { return; }
            _modelView *= new Matrix4(1, 0, 0, 0,
                                      0, 1, 0, 0,
                                      0, 0, 1, 0,
                                      x, y, z, 1);
        }

        public void MultiplyScale(float scale)
        {
            _modelView *= new Matrix4(scale, 0, 0, 0,
                                      0, scale, 0, 0,
                                      0, 0, scale, 0,
                                      0, 0, 0, 1);
        }

        public void MultiplyScale(float x, float y, float z)
        {
            _modelView *= new Matrix4(x, 0, 0, 0,
                                      0, y, 0, 0,
                                      0, 0, z, 0,
                                      0, 0, 0, 1);
        }

        public void MultiplyScale(float scale, Vector3 scaleOrigin)
        {
            Translate(-scaleOrigin.X, -scaleOrigin.Y, -scaleOrigin.Z);
            MultiplyScale(scale);
            Translate(scaleOrigin.X, scaleOrigin.Y, scaleOrigin.Z);
        }

        public void MultiplyScale(float x, float y , float z, Vector3 scaleOrigin)
        {
            Translate(-scaleOrigin.X, -scaleOrigin.Y, -scaleOrigin.Z);
            MultiplyScale(x, y , z);
            Translate(scaleOrigin.X, scaleOrigin.Y, scaleOrigin.Z);
        }
    }
}
