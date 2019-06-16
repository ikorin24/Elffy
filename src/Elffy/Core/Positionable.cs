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
        private Quaternion _rotation = Quaternion.Identity;
        private Matrix4 _modelMatrix = Matrix4.Identity;

        internal Quaternion Rotation => _rotation;
        internal Matrix4 ModelMatrix => _modelMatrix;

        public ObjectLayer Layer { get; set; }

        public Vector3 Position
        {
            get { return _modelMatrix.Row3.Xyz; }
            set { _modelMatrix.Row3.Xyz = value; }
        }

        public float ScaleX
        {
            get { return _modelMatrix.Row0.X; }
            set { _modelMatrix.Row0.X = value; }
        }

        public float ScaleY
        {
            get { return _modelMatrix.Row1.Y; }
            set { _modelMatrix.Row1.Y = value; }
        }

        public float ScaleZ
        {
            get { return _modelMatrix.Row2.Z; }
            set { _modelMatrix.Row2.Z = value; }
        }

        public void Translate(float x, float y, float z)
        {
            if(x == 0 && y == 0 && z == 0) { return; }
            _modelMatrix *= new Matrix4(1, 0, 0, 0,
                                      0, 1, 0, 0,
                                      0, 0, 1, 0,
                                      x, y, z, 1);
        }

        public void MultiplyScale(float scale)
        {
            _modelMatrix *= new Matrix4(scale, 0, 0, 0,
                                      0, scale, 0, 0,
                                      0, 0, scale, 0,
                                      0, 0, 0, 1);
        }

        public void MultiplyScale(float x, float y, float z)
        {
            _modelMatrix *= new Matrix4(x, 0, 0, 0,
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

        public void Rotate(Quaternion quaternion)
        {
            _rotation *= quaternion;
        }
    }
}
