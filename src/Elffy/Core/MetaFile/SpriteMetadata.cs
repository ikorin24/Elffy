using Elffy.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core.MetaFile
{
    /// <summary>スプライトのメタデータクラス</summary>
    public class SpriteMetadata : Metadata
    {
        /// <summary>テクスチャのリソース名</summary>
        public string TextureResource { get; private set; }

        /// <summary>スプライトの画像数</summary>
        public int PageCount { get; private set; }

        /// <summary>統合画像内に並べられたX方向の画像数</summary>
        public int XCount { get; private set; }

        /// <summary>統合画像内に並べられたY方向の画像数</summary>
        public int YCount { get; private set; }

        /// <summary>スプライトのピクセル幅 (統合画像内の1つの画像のピクセル幅)</summary>
        public int PixelWidth { get; private set; }

        /// <summary>スプライトのピクセル高 (統合画像内の1つの画像のピクセル高)</summary>
        public int PixelHeight { get; private set; }

        /// <summary>デシリアライズされたデータを使って初期化を行います</summary>
        /// <param name="data">デシリアライズされたデータ</param>
        protected override void InitializeFromDeserialized(MetadataDeserialized data)
        {
            base.InitializeFromDeserialized(data);
            ExceptionManager.ThrowIf(data.DataType != MetadataType.Sprite, new InvalidDataException());
            ExceptionManager.ThrowIf(data.DataContents?.Length != 1, new InvalidDataException());
            ExceptionManager.CheckType<SpriteInfoDeserialized, object>(data.DataContents[0], $"Metadata type is invalid.");

            var info = (SpriteInfoDeserialized)data.DataContents[0];
            ExceptionManager.ThrowIf(info.XCount <= 0, new InvalidDataException($"{nameof(info.XCount)} is 0 or negative."));
            ExceptionManager.ThrowIf(info.YCount <= 0, new InvalidDataException($"{nameof(info.YCount)} is 0 or negative."));
            ExceptionManager.ThrowIf(info.PageCount <= 0, new InvalidDataException($"{nameof(info.PageCount)} is 0 or negative."));
            ExceptionManager.ThrowIf(info.PixelWidth <= 0, new InvalidDataException($"{nameof(info.PixelWidth)} is 0 or negative."));
            ExceptionManager.ThrowIf(info.PixelHeight <= 0, new InvalidDataException($"{nameof(info.PixelHeight)} is 0 or negative."));
            TextureResource = info.TextureResource;
            PageCount = info.PageCount;
            XCount = info.XCount;
            YCount = info.YCount;
            PixelWidth = info.PixelWidth;
            PixelHeight = info.PixelHeight;
        }
    }
}
