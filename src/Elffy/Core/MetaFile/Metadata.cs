#nullable enable
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Serialization;
using System;
using System.IO;

namespace Elffy.Core.MetaFile
{
    /// <summary>メタデータを表すクラス</summary>
    public abstract class Metadata
    {
        private static DataSerializer _serializer => (_s ??= new DataSerializer());
        private static DataSerializer? _s;

        /// <summary>メタデータのタイプを取得します</summary>
        public MetadataType DataType { get; private set; }

        /// <summary>ストリームから指定の型のメタデータを読み取ります</summary>
        /// <typeparam name="T">メタデータの型</typeparam>
        /// <param name="stream">メタデータを読み取るストリーム</param>
        /// <returns>読み取ったメタデータ</returns>
        public static T LoadFromStream<T>(Stream stream) where T : Metadata, new()
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }
            var data = _serializer.Deserialize<MetadataDeserialized>(stream);
            var metaFile = new T();
            metaFile.InitializeFromDeserialized(data);
            return metaFile;
        }

        /// <summary>デシリアライズされたデータを使って初期化を行います</summary>
        /// <param name="data">デシリアライズされたデータ</param>
        protected virtual void InitializeFromDeserialized(MetadataDeserialized data)
        {
            if(!IsSupportedVersion(data)) { throw new FormatException($"Not supported format version : '{data.FormatVersion}'"); }
            DataType = data.DataType;
        }

        /// <summary>この <see cref="MetadataDeserialized"/> のフォーマットバージョンがサポートされているかを取得します</summary>
        /// <returns>フォーマットバージョンがサポートされているかどうか</returns>
        private bool IsSupportedVersion(MetadataDeserialized data)
        {
            const string SUPPORTED = "1.0";
            return data.FormatVersion == SUPPORTED;
        }
    }
}
