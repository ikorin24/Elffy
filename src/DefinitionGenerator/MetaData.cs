using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DefinitionGenerator
{
    public static class MetaData
    {
        private const string META_TAG = "elffy-tool";
        private const string USINGS_NODE = "Usings";
        private const string NAMESPACE = "Namespace";
        private const string GENERIC_ATTRIBUTE = "Generic";
        private const string ARRAY_ATTRIBUTE = "Array";
        private const string NAME_ATTRIBUTE = "Name";
        private const string MODIFIER_ATTRIBUTE = "Modifier";

        public static bool IsMetaDataNode(XElement element) => element?.Name?.NamespaceName == META_TAG;

        public static bool IsMetaDataAttribute(XAttribute attribute) => attribute?.Name?.NamespaceName == META_TAG;

        public static bool IsUsingsNode(XElement element) => IsMetaDataNode(element) && element?.Name?.LocalName == USINGS_NODE;

        public static bool IsUsingNode(XElement element) => IsMetaDataNode(element) && element?.Name?.LocalName == nameof(Using);

        public static Using GetUsing(XElement element) => new Using(element.Attribute(NAMESPACE).Value);

        public static string GetGenericType(XElement element) => element.Attribute(XName.Get(GENERIC_ATTRIBUTE, META_TAG))?.Value;

        public static bool IsArray(XElement element) => element.Attribute(XName.Get(ARRAY_ATTRIBUTE, META_TAG))?.Value?.ToLower() == "true";

        public static string GetVariableName(XElement element) => element.Attribute(XName.Get(NAME_ATTRIBUTE, META_TAG))?.Value;

        public static string GetModifier(XElement element) => element.Attribute(XName.Get(MODIFIER_ATTRIBUTE, META_TAG))?.Value;
    }
}
