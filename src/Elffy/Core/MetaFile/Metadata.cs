﻿using Elffy.Exceptions;
using Elffy.Serialization;
using System;
using System.IO;

namespace Elffy.Core.MetaFile
{
    /// <summary>メタデータを表すクラス</summary>
    public class Metadata
    {
        private static DataSerializer _serializer;

        /// <summary>メタデータのタイプを取得します</summary>
        public MetadataType DataType { get; private set; }

        /// <summary>ストリームから指定の型のメタデータを読み取ります</summary>
        /// <typeparam name="T">メタデータの型</typeparam>
        /// <param name="stream">メタデータを読み取るストリーム</param>
        /// <returns>読み取ったメタデータ</returns>
        public static T LoadFromStream<T>(Stream stream) where T : Metadata, new()
        {
            ArgumentChecker.ThrowIfNullArg(stream, nameof(stream));
            if(_serializer == null) {
                _serializer = new DataSerializer();
            }
            var data = _serializer.Deserialize<MetadataDeserialized>(stream);
            var metaFile = new T();
            metaFile.InitializeFromDeserialized(data);
            return metaFile;
        }

        /// <summary>デシリアライズされたデータを使って初期化を行います</summary>
        /// <param name="data">デシリアライズされたデータ</param>
        protected virtual void InitializeFromDeserialized(MetadataDeserialized data)
        {
            ArgumentChecker.ThrowIf(!IsSupportedVersion(data), new FormatException($"Not supported format version : '{data.FormatVersion}'"));
            DataType = data.DataType;
        }

        /// <summary>この <see cref="MetadataDeserialized"/> のフォーマットバージョンがサポートされているかを取得します</summary>
        /// <returns>フォーマットバージョンがサポートされているかどうか</returns>
        public bool IsSupportedVersion(MetadataDeserialized data)
        {
            const string SUPPORTED = "1.0";
            return data.FormatVersion == SUPPORTED;
        }
    }
}