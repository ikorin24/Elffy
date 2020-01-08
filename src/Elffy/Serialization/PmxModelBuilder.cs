#nullable enable
using System;
using System.IO;
using Elffy.Shape;
using Elffy.Effective;
//using MMDTools;


namespace Elffy.Serialization
{
    internal static class PmxModelBuilder
    {
        public static Model3D LoadModel(Stream stream)
        {
            throw new NotImplementedException();
            //var pmx = PMXParser.Parse(stream);
            //using(var vertexArray = new UnmanagedArray<Core.Vertex>(pmx.VertexList.Count))
            //using(var indexArray = new UnmanagedArray<int>(pmx.SurfaceList.Count * 3)) {
            //    for(int i = 0; i < pmx.VertexList.Count; i++) {
            //        var v = pmx.VertexList[i];
            //        vertexArray[i] = new Core.Vertex(GetVector(v.Posision), GetVector(v.Normal), GetVector(v.UV));
            //    }
            //    for(int i = 0; i < pmx.SurfaceList.Count; i++) {
            //        var s = pmx.SurfaceList[i];
            //        indexArray[i * 3] = s.V1;
            //        indexArray[i * 3 + 1] = s.V2;
            //        indexArray[i * 3 + 2] = s.V3;
            //    }
            //    return new Model3D(vertexArray, indexArray);
            //}
        }

        //private static OpenTK.Vector3 GetVector(Vector3 v) => new OpenTK.Vector3(v.X, v.Y, v.Z);
        //private static OpenTK.Vector2 GetVector(Vector2 v) => new OpenTK.Vector2(v.X, v.Y);
    }
}
