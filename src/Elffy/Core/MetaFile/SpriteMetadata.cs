#nullable enable
using Elffy.Exceptions;

namespace Elffy.Core.MetaFile
{
    /// <summary>スプライトのメタデータクラス</summary>
    public class SpriteMetadata : Metadata
    {
        /// <summary>テクスチャのリソース名</summary>
        public string TextureResource { get; private set; } = string.Empty;

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
            DataChecker.ThrowInvalidDataIf(data.DataType != MetadataType.Sprite, "meta data is not sprite");
            DataChecker.ThrowInvalidDataIf(data.DataContents?.Length != 1, "invalid data format");
            ArgumentChecker.CheckType<SpriteInfoDeserialized, object>(data.DataContents![0], "Metadata type is invalid.");
            var info = (SpriteInfoDeserialized)data.DataContents[0];
            DataChecker.ThrowInvalidDataIf(info.XCount <= 0, $"{nameof(info.XCount)} is 0 or negative.");
            DataChecker.ThrowInvalidDataIf(info.YCount <= 0, $"{nameof(info.YCount)} is 0 or negative.");
            DataChecker.ThrowInvalidDataIf(info.PageCount <= 0, $"{nameof(info.PageCount)} is 0 or negative.");
            DataChecker.ThrowInvalidDataIf(info.PixelWidth <= 0, $"{nameof(info.PixelWidth)} is 0 or negative.");
            DataChecker.ThrowInvalidDataIf(info.PixelHeight <= 0, $"{nameof(info.PixelHeight)} is 0 or negative.");
            TextureResource = info.TextureResource;
            PageCount = info.PageCount;
            XCount = info.XCount;
            YCount = info.YCount;
            PixelWidth = info.PixelWidth;
            PixelHeight = info.PixelHeight;
        }
    }
}
