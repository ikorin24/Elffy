#nullable enable
using Elffy.Exceptions;
using Elffy.Shape;
using System;
using System.IO;

namespace Elffy.Serialization
{
    public static class ModelLoader
    {
        /// <summary>3Dモデルファイルの種類を指定して、ストリームから3Dモデルを読み込みます</summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="type">3Dモデルの種類</param>
        /// <returns>読み込んだ3Dモデル</returns>
        [Obsolete("一応残しておきますが使えません", true)]
        public static Model3D Load(Stream stream, ModelType type)
        {
            ArgumentChecker.ThrowIfNullArg(stream, nameof(stream));
            throw new NotImplementedException();
            //return type switch
            //{
            //    ModelType.Fbx => FbxModelBuilder.LoadModel(stream),
            //    ModelType.Pmx => PmxModelBuilder.LoadModel(stream),
            //    _ => throw new NotSupportedException($"Model type '{type}' is not supported."),
            //};
        }
    }

    /// <summary>3Dモデルの種類を表します</summary>
    public enum ModelType
    {
        /// <summary>fbx ファイル</summary>
        Fbx,
        /// <summary>pmx file</summary>
        Pmx,
        /// <summary>obj ファイル</summary>
        Obj,
    }
}
