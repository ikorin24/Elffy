using System.Xml.Serialization;

namespace Elffy.Core.MetaFile
{
    /// <summary>エンジンが扱うメタデータを表すシリアライズ用クラス</summary>
    [XmlInclude(typeof(SpriteInfoDeserialized))]
    [XmlType("Metadata")]
    public sealed class MetadataDeserialized
    {
        /// <summary>フォーマットバージョン</summary>
        [XmlAttribute("FormatVersion")]
        public string FormatVersion { get; set; } = "1.0";

        /// <summary>データタイプ</summary>
        [XmlElement("DataType")]
        public MetadataType DataType { get; set; }

        /// <summary>この <see cref="MetadataDeserialized"/> が持つデータ</summary>
        [XmlArray("DataContents")]
        [XmlArrayItem(typeof(SpriteInfoDeserialized))]
        public object[] DataContents { get; set; }

        internal MetadataDeserialized()
        {
        }
    }
}
