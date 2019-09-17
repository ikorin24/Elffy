using Elffy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DefinitionGenerator
{
    public static class DefinitionParser
    {
        private const string META_TAG = "elffy-tool";
        private const string USINGS_NODE = "Usings";
        private const string NAMESPACE = "Namespace";
        private const string GENERIC_ATTRIBUTE = "Generic";
        private const string ARRAY_ATTRIBUTE = "Array";

        public static DefinitionContent Parse(string filename)
        {
            if(filename == null) { throw new ArgumentNullException(nameof(filename)); }
            using(var stream = File.OpenRead(filename)) {
                return Parse(stream);
            }
        }

        public static DefinitionContent Parse(Stream stream)
        {
            if(stream == null) { throw new ArgumentNullException(nameof(stream)); }
            var xml = XElement.Load(stream);

            // Depth-first search
            IEnumerable<XElement> GetAllElements(XElement root)
            {
                yield return root;
                foreach(var element in root.Elements().SelectMany(e => GetAllElements(e))) {
                    yield return element;
                }
            }

            var elements = GetAllElements(xml).ToArray();
            var content = CreateContent(elements);
            return content;
        }

        #region CreateUsings
        private static Using[] CreateUsings(IList<XElement> elements)
        {
            var usingRoot = elements.FirstOrDefault(elem => elem.Name.Namespace == META_TAG && elem.Name.LocalName == USINGS_NODE);
            if(usingRoot == null) {
                return new Using[0];
            }
            var usings = usingRoot.Elements().Where(elem => elem.Name.Namespace == META_TAG && elem.Name.LocalName == nameof(Using))
                                             .Select(elem => new Using(elem.Attribute(NAMESPACE).Value))
                                             .ToArray();
            return usings;
        }
        #endregion

        private static DefinitionContent CreateContent(IList<XElement> elements)
        {
            var usings = CreateUsings(elements);

            // 変数になるもの
            var variables = elements.Skip(1)
                                    .Where(element => element.Name.NamespaceName != META_TAG)       // メタ情報を除く
                                    .Where(element => !element.Name.LocalName.Contains('.'))        // プロパティノードを除く
                                    .Select((element, i) => 
            {
                var typeName = GetTypeName(element, usings);
                var variable = new Variable($"_var{i}", typeName);
                variable.Accessability = GetAccessability(element);
                return (variable, element);
            }).ToDictionary(x => x.element, x => x.variable);


            //Assembly.LoadFrom("hoge").

            var setProperties = variables.SelectMany(x =>
            {
                var variable = x.Value;
                var element = x.Key;
                return element.Attributes()
                              .Where(a => a.Name.NamespaceName != META_TAG)
                              .Select(a => (PropName: a.Name.LocalName, Value: a.Value))
                              .Select(p => $"{variable.Name}.{p.PropName} = {variable.Name}.{p.PropName}.FromAltString(\"{p.Value}\");");
            }).ToArray();

            // どのプロパティにどのオブジェクトが代入されるかの依存関係
            var dependencies = elements.Skip(1)
                                       .Where(element => element.Name.NamespaceName != META_TAG)
                                       .Where(element => element.Name.LocalName.Contains('.'))
                                       .SelectMany(element =>
            {
                var propertyName = element.Name.LocalName.Split('.')[1];
                return element.Elements().Select(child => new Dependency(variables[element.Parent], propertyName, variables[child]));
            }).ToArray();

            // 同じプロパティ名に代入しているものをまとめる
            //dependencies.GroupBy(d => $"{d.Owner.Name}.{d.Property}")
            //            .Select(g => 
            //{
            //    if(g.Count() == 1) {
            //        return g.First();
            //    }
            //    else {

            //    }
            //})

            return new DefinitionContent(usings, variables.Values.ToArray(), setProperties, dependencies);
        }

        private static string GetTypeName(XElement element, IList<Using> usings)
        {
            // <List x:Generic="int" x:Array="True"/>
            // ↓
            // List<int>[]

            // <List x:Generic="int[]"/>
            // ↓
            // List<int[]>

            // <List x:Generic="int[]" x:Array="True"/>
            // ↓
            // List<int[]>[]

            var genericType = element.Attribute(XName.Get(GENERIC_ATTRIBUTE, META_TAG))?.Value;
            var isArray = element.Attribute(XName.Get(ARRAY_ATTRIBUTE, META_TAG))?.Value?.ToLower() == "true";

            if(genericType != null) {
                if(isArray) {
                    return $"{element.Name.LocalName}<{genericType}>[]";
                }
                else {
                    return $"{element.Name.LocalName}<{genericType}>";
                }
            }
            else {
                if(isArray) {
                    return $"{element.Name.LocalName}[]";
                }
                else {
                    return element.Name.LocalName;
                }
            }
        }

        private static void GetPropertyValue(XElement element, Using[] usings)
        {
            var type = element.Name.LocalName;
            var props = element.Attributes()
                               .Where(x => x.Name.NamespaceName != META_TAG)
                               .Select(x => (PropName: x.Name.LocalName, Value: x.Value))
                               .ToArray();
            // hoge.Property = new type().FromAltString({prop.Value});

            //Assembly.LoadFrom("hoge").
        }

        private static string GetAccessability(XElement element)
        {
            // NOTE: 現在は private のみ
            return "private";
        }
    }
}
