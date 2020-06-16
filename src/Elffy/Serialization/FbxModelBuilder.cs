#nullable enable
using Elffy.Core;
using Elffy.Shape;
using OpenToolkit;
using OpenToolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elffy.Serialization
{
    internal static class FbxModelBuilder
    {
        #region LoadModel
        /// <summary>Fbxファイルから3Dモデルを作ります</summary>
        /// <param name="stream">fbxのストリーム</param>
        /// <returns>3Dモデル</returns>
        public static Model3D LoadModel(Stream stream)
        {
            #region 定数
            const string OBJECTS = "Objects";

            const string GEOMETRY = "Geometry";
            const string VERTICES = "Vertices";
            const string VERTEX_INDEX = "PolygonVertexIndex";
            const string NORMAL_INFO = "LayerElementNormal";
            const string NORMAL = "Normals";
            const string MATERIAL_INFO = "LayerElementMaterial";
            const string MATERIAL_INDEX = "Materials";

            const string MATERIAL_DEFINITION = "Material";
            const string SHADING_MODEL = "ShadingModel";
            const string MATERIAL_DATA = "Properties70";
            #endregion

            var parser = new FbxParser();
            var fbx = parser.Parse(stream);
            var objectNode = fbx.Children.Find(x => x.Name == OBJECTS);

            #region ポリゴン情報取得
            var geometries = objectNode.Children.FindAll(x => x.Name == GEOMETRY).Select(geometry => {
                var id = ((FbxLongProperty)geometry.Properties[0]).Value;
                var name = ((FbxStringProperty)geometry.Properties[1]).Value;
                double[]? vertPositions = default;
                int[]? indexes = default;
                double[]? normals = default;
                int[]? materialIndex = default;

                foreach(var node in geometry.Children) {
                    switch(node.Name) {
                        case VERTICES:
                            // TODO: マイナスのインデックス
                            vertPositions = ((FbxDoubleArrayProperty)node.Properties[0]).Value;
                            break;
                        case VERTEX_INDEX:
                            indexes = ((FbxIntArrayProperty)node.Properties[0]).Value;
                            break;
                        case NORMAL_INFO:
                            normals = ((FbxDoubleArrayProperty)node.Children.Find(x => x.Name == NORMAL).Properties[0]).Value;
                            break;
                        case MATERIAL_INFO:
                            materialIndex = ((FbxIntArrayProperty)node.Children.Find(x => x.Name == MATERIAL_INDEX).Properties[0]).Value;
                            break;
                        default:
                            break;
                    }
                }

                if(vertPositions == null) { throw new FormatException("No vertex points"); }
                if(indexes == null) { throw new FormatException("No indexes"); }
                if(normals == null) { throw new FormatException("No normals"); }
                if(materialIndex == null) { throw new FormatException("No material Index"); }

                return (id, name, vertPositions, indexes, normals, materialIndex);
            });
            #endregion

            #region マテリアル情報取得
            var materials = objectNode.Children.FindAll(x => x.Name == MATERIAL_DEFINITION).Select(material => {
                var id = ((FbxLongProperty)material.Properties[0]).Value;
                var name = ((FbxStringProperty)material.Properties[1]).Value;
                string? shadingModel = default;

                Color4 emitColor = default;
                double emit = default;
                Color4 ambientColor = default;
                Color4 diffuseColor = default;
                double diffuse = default;
                Color4 specularColor = default;
                double specular = default;
                double shininess = default;

                foreach(var node in material.Children) {
                    switch(node.Name) {
                        case SHADING_MODEL:
                            shadingModel = ((FbxStringProperty)node.Properties[0]).Value;
                            break;
                        case MATERIAL_DATA:
                            (emitColor, emit, ambientColor, diffuseColor, diffuse, specularColor, specular, shininess) = GetMaterialValues(node);
                            break;
                        default:
                            break;
                    }
                }

                return (id, name, shadingModel, emitColor, emit, ambientColor, diffuseColor, diffuse, specularColor, specular, shininess);
            });
            #endregion

            var modelVertices = new List<Vertex>();  // 頂点配列
            var modelIndexes = new List<int>();   // 三角形ポリゴンインデックス配列
            var indexOffset = 0;

            // 複数のジオメトリをまとめて一つの頂点配列と三角形ポリゴンインデックス配列にする
            foreach(var geometry in geometries) {
                var positions = BuildVectorArray(geometry.vertPositions);   // 座標配列
                var normals = BuildVectorArray(geometry.normals);           // 頂点の法線配列
                if(geometry.indexes.Length != normals.Length) { throw new FormatException(); }   // 法線配列長はインデックス配列長と同じになっているはず
                var (vertices, indexes) = SolveVertices(geometry.indexes, normals, positions);
                modelVertices.AddRange(vertices);
                if(indexOffset != 0) {
                    for(int i = 0; i < indexes.Count; i++) {
                        indexes[i] += indexOffset;
                    }
                }
                indexOffset = modelVertices.Count;
                modelIndexes.AddRange(indexes);
            }
            // パフォーマンスとかもう少し考える必要がある (Span<T>)
            var model = new Model3D(modelVertices.ToArray().AsSpan(), modelIndexes.ToArray().AsSpan());
            return model;
        }
        #endregion

        #region private Method
        #region GetMaterialValues
        private static (Color4, double, Color4, Color4, double, Color4, double, double) GetMaterialValues(FbxNode materialData)
        {
            #region 定数
            const string EMIT_COLOR = "EmissiveColor";
            const string EMIT_VALUE = "EmissiveFactor";
            const string AMBIENT_COLOR = "AmbientColor";
            const string DIFFUSE_COLOR = "DiffuseColor";
            const string DIFFUSE_VALUE = "DiffuseFactor";
            const string SPECULAR_COLOR = "SpecularColor";
            const string SPECULAR_VALUE = "SpecularFactor";
            const string SHININESS = "Shininess";
            #endregion

            #region local func
            Color4 GetColor(FbxNode node)
            {
                var r = (float)((FbxDoubleProperty)node.Properties[4]).Value;
                var g = (float)((FbxDoubleProperty)node.Properties[5]).Value;
                var b = (float)((FbxDoubleProperty)node.Properties[6]).Value;
                return new Color4(r, g, b, 1f);
            }

            double GetDouble(FbxNode node)
            {
                return ((FbxDoubleProperty)node.Properties[4]).Value;
            }
            #endregion

            Color4 emitColor = default;
            double emit = default;
            Color4 ambientColor = default;
            Color4 diffuseColor = default;
            double diffuse = default;
            Color4 specularColor = default;
            double specular = default;
            double shininess = default;
            foreach(var node in materialData.Children) {
                switch(node.Name) {
                    case EMIT_COLOR:
                        emitColor = GetColor(node);
                        break;
                    case EMIT_VALUE:
                        emit = GetDouble(node);
                        break;
                    case AMBIENT_COLOR:
                        ambientColor = GetColor(node);
                        break;
                    case DIFFUSE_COLOR:
                        diffuseColor = GetColor(node);
                        break;
                    case DIFFUSE_VALUE:
                        diffuse = GetDouble(node);
                        break;
                    case SPECULAR_COLOR:
                        specularColor = GetColor(node);
                        break;
                    case SPECULAR_VALUE:
                        specular = GetDouble(node);
                        break;
                    case SHININESS:
                        shininess = GetDouble(node);
                        break;
                    default:
                        break;
                }
            }
            return (emitColor, emit, ambientColor, diffuseColor, diffuse, specularColor, specular, shininess);
        }
        #endregion

        #region BuildVectorArray
        /// <summary>1次元配列から (x,y,z)配列を作ります</summary>
        /// <param name="rawArray">配列</param>
        /// <returns>ベクトル配列</returns>
        private static Vector3[] BuildVectorArray(double[] rawArray)
        {
            if(rawArray == null) { return new Vector3[0]; }
            if(rawArray.Length % 3 != 0) { throw new FormatException(); }   // x,y,zの連続なので3の倍数のはず
            var vertices = new Vector3[rawArray.Length / 3];
            unsafe {
                var len = rawArray.Length / 3;
                for(int i = 0; i < len; i++) {
                    int j = i * 3;
                    float x = (float)rawArray[j];
                    float y = (float)rawArray[j + 1];
                    float z = (float)rawArray[j + 2];
                    vertices[i] = new Vector3(x, y, z);
                }
            }
            return vertices;
        }
        #endregion

        #region SolveVertices
        /// <summary>頂点配列と三角形ポリゴンインデックスを生成します</summary>
        /// <param name="indexes">fbxから読み取ったindexd</param>
        /// <param name="normals">法線配列</param>
        /// <param name="positions">座標配列</param>
        /// <returns>頂点配列と三角形ポリゴンインデックス</returns>
        private static (Vertex[] vertices, IList<int> indexes) SolveVertices(int[] indexes, Vector3[] normals, Vector3[] positions)
        {
            // positions の数は幾何的な頂点の数
            // normals の数は属性が同じ頂点の数 (属性: 座標・法線・頂点色など) (== indexes の数)
            // 頂点属性の数になるように、positions を拡張する

            var last = new bool[normals.Length];
            var vertices = new Vertex[normals.Length];
            unsafe {
                for(int i = 0; i < indexes.Length; i++) {
                    last[i] = indexes[i] < 0;
                    var index = last[i] ? (-indexes[i] - 1) : indexes[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)
                    vertices[i] = new Vertex(positions[index], normals[i], default);
                }
            }
            return (vertices, GenerateIndexArray(last));
        }
        #endregion

        #region GenerateIndexArray
        /// <summary>三角形ポリゴンインデックスを生成します</summary>
        /// <param name="isPolygonLastArray">その頂点が多角形ポリゴンの最終頂点かどうかを表す配列</param>
        /// <returns>三角形ポリゴンインデックス</returns>
        private static IList<int> GenerateIndexArray(bool[] isPolygonLastArray)
        {
            if(isPolygonLastArray == null) { return new int[0]; }
            int num = 0;
            var indexes = new List<int>();
            var buf = new List<int>();

            // 負のインデックスは多角形の最後の頂点を表す。
            // 正しいインデックスの2の補数になっているので、ビット反転して1を引くと正しい値に戻る
            foreach(var isLast in isPolygonLastArray) {
                buf.Add(num++);
                if(isLast) {
                    if(buf.Count <= 2) { throw new FormatException(); }     // 三角形以上の多角形であるはずなので、要素数がたりないならフォーマットエラー
                    unsafe {
                        for(int j = 0; j < buf.Count - 2; j++) {
                            indexes.Add(buf[0]);
                            indexes.Add(buf[j + 1]);
                            indexes.Add(buf[j + 2]);
                        }
                    }
                    buf.Clear();
                }
            }
            return indexes;
        }
        #endregion
        #endregion
    }
}
