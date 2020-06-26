#nullable enable
using System.Collections.Generic;
using System.Xml.Serialization;
using Elffy;
using Elffy.AssemblyServices;

namespace Sandbox
{
    [XmlRoot("SaveData")]
    public class SaveData : ConfigBase<SaveData>
    {
        private static readonly string PATH = System.IO.Path.Combine(AssemblyState.EntryAssemblyDirectory, "SaveData.xml");

        public SaveData()
        {
            Path = PATH;
        }

        [XmlArray("Blocks")]
        [XmlArrayItem("Block")]
        public List<SaveBlock> Blocks { get; set; } = new List<SaveBlock>();
    }

    public class SaveBlock
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;
    }
}
