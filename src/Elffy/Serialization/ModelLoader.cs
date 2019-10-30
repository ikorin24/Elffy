using Elffy.Exceptions;
using Elffy.Shape;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Serialization
{
    public static class ModelLoader
    {
        #region Load
        /// <summary>3Dモデルファイルの種類を指定して、ストリームから3Dモデルを読み込みます</summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="type">3Dモデルの種類</param>
        /// <returns>読み込んだ3Dモデル</returns>
        public static Model3D Load(Stream stream, ModelType type)
        {
            ArgumentChecker.ThrowIfNullArg(stream, nameof(stream));
            try {
                switch(type) {
                    case ModelType.Fbx:
                        return FbxModelBuilder.LoadModel(stream);
                    default:
                        throw new NotSupportedException($"Model type '{type}' is not supported.");
                }
            }
            catch(Exception ex) {
                throw new FormatException("Failed in loading.", ex);
            }
        }
        #endregion
    }

    #region enum ModelType
    /// <summary>3Dモデルの種類を表します</summary>
    public enum ModelType
    {
        /// <summary>fbx ファイル</summary>
        Fbx,
        /// <summary>obj ファイル</summary>
        Obj,
    }
    #endregion
}
