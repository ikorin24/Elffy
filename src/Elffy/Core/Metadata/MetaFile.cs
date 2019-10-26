using System.Xml.Serialization;

namespace Elffy.Core.Metadata
{
    /// <summary>エンジンが扱うメタデータを表すシリアライズ用クラス</summary>
    [XmlInclude(typeof(SpriteInfo))]
    [XmlType("MetaFile")]
    public class MetaFile
    {
        /// <summary>フォーマットバージョン</summary>
        [XmlAttribute("FormatVersion")]
        public string FormatVersion { get; set; } = "1.0";

        /// <summary>ファイルタイプ</summary>
        [XmlElement("FileType")]
        public MetaFileType FileType { get; set; }

        /// <summary>この <see cref="MetaFile"/> が持つデータ</summary>
        [XmlArray("Metadata")]
        [XmlArrayItem(typeof(SpriteInfo))]
        public object[] Metadata { get; set; }

        /// <summary>この <see cref="MetaFile"/> のフォーマットバージョンがサポートされているかを取得します</summary>
        /// <returns>フォーマットバージョンがサポートされているかどうか</returns>
        public bool IsSupportedVersion()
        {
            const string SUPPORTED = "1.0";
            return FormatVersion == SUPPORTED;
        }
    }
}
