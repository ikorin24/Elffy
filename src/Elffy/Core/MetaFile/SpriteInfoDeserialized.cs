using System.Xml.Serialization;

namespace Elffy.Core.MetaFile
{
    /// <summary>スプライト画像情報を表す <see cref="MetadataDeserialized"/> の <see cref="MetadataDeserialized.DataContents"/> </summary>
    [XmlType("SpriteInfo")]
    public class SpriteInfoDeserialized
    {
        /// <summary>テクスチャのリソース名</summary>
        [XmlElement("TextureResource")]
        public string TextureResource { get; set; }

        /// <summary>スプライトの画像数</summary>
        [XmlElement("PageCount")]
        public int PageCount { get; set; }

        /// <summary>統合画像内に並べられたX方向の画像数</summary>
        [XmlElement("XCount")]
        public int XCount { get; set; }

        /// <summary>統合画像内に並べられたY方向の画像数</summary>
        [XmlElement("YCount")]
        public int YCount { get; set; }

        /// <summary>スプライトのピクセル幅 (統合画像内の1つの画像のピクセル幅)</summary>
        [XmlElement("PixelWidth")]
        public int PixelWidth { get; set; }

        /// <summary>スプライトのピクセル高 (統合画像内の1つの画像のピクセル高)</summary>
        [XmlElement("PixelHeight")]
        public int PixelHeight { get; set; }

        internal SpriteInfoDeserialized()
        {
        }
    }
}
