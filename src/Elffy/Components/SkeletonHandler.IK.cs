#nullable enable
using System;
using Elffy.Mathematics;

namespace Elffy.Components
{
    unsafe partial struct SkeletonHandler
    {
        public void MoveBoneCCDIK(int index, Vector3 target, int iterationCount)
        {
            if((uint)index >= (uint)_length) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            for(int i = 0; i < iterationCount; i++) {
                for(var current = &_tree[index]; current != null; current = current->Parent) {
                    if(current == null) { break; }
                    var baseBoneInfo = current->Parent;
                    Vector3 basePos;
                    if(baseBoneInfo != null) {
                        var baseIndex = baseBoneInfo->ID;
                        basePos = (_translation[baseIndex] * _pos[baseIndex] * new Vector4(0, 0, 0, 1)).Xyz;
                    }
                    else {
                        basePos = Vector3.Zero;
                    }
                    var pos = (_translation[index] * _pos[index] * new Vector4(0, 0, 0, 1)).Xyz;
                    var v1 = pos - basePos;
                    var v2 = target - basePos;
                    var theta = MathF.Acos(Vector3.Dot(v1, v2));
                    if(theta >= 1f.ToDegree()) {
                        var axis = Vector3.Cross(v1, v2).Normalized();
                        _translation[index] = Matrix4.FromAxisAngle(axis, theta) * _translation[index];
                    }
                }
            }
            return;
        }
    }
}
