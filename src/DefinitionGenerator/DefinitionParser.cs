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
        #region Parse
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
            var content = CreateContent(xml);
            return content;
        }
        #endregion

        #region CreateUsings
        private static Using[] CreateUsings(IList<XElement> elements)
        {
            var usingRoot = elements.FirstOrDefault(MetaData.IsUsingsNode);
            if(usingRoot == null) {
                return new Using[0];
            }
            var usings = usingRoot.Elements().Where(MetaData.IsUsingNode)
                                             .Select(MetaData.GetUsing)
                                             .ToArray();
            return usings;
        }
        #endregion

        #region CreateContent
        private static DefinitionContent CreateContent(XElement xml)
        {
            // Depth-first search
            IEnumerable<XElement> GetAllElements(XElement root)
            {
                yield return root;
                foreach(var element in root.Elements().SelectMany(e => GetAllElements(e))) {
                    yield return element;
                }
            }

            var elements = GetAllElements(xml).ToArray();

            var usings = CreateUsings(elements);

            // 変数になるもの
            var variables = elements.Skip(1)
                                    .Where(element => !MetaData.IsMetaDataNode(element))        // メタ情報を除く
                                    .Where(element => !element.Name.LocalName.Contains('.'))    // プロパティノードを除く
                                    .Select((element, i) => 
            {
                var typeName = GetTypeName(element);
                var variableName = MetaData.GetVariableName(element) ?? $"__var{i}";
                var variable = new Variable(variableName, typeName);
                variable.Accessability = MetaData.GetModifier(element) ?? "private";
                return (variable, element);
            }).ToDictionary(x => x.element, x => x.variable);

            var setProperties = variables.SelectMany(x =>
            {
                var variable = x.Value;
                var element = x.Key;
                return element.Attributes()
                              .Where(a => !MetaData.IsMetaDataAttribute(a))
                              .Select(a => (PropName: a.Name.LocalName, Value: a.Value))
                              .Select(p => $"{variable.Name}.{p.PropName} = FromAltString({variable.Name}.{p.PropName}, \"{p.Value}\");");
            })
            .Concat(
                elements.Skip(1)
                        .Where(element => !MetaData.IsMetaDataNode(element))
                        .Where(element => element.Name.LocalName.Contains('.'))         // プロパティノードについて
                        .Select(element =>
            {
                var owner = variables[element.Parent];
                var propertyName = element.Name.LocalName.Split('.')[1];
                var value = variables[element.Elements().First()];
                return $"{owner.Name}.{propertyName} = {value.Name};";
            }))
            .ToArray();


            // どのプロパティにどのオブジェクトが代入されるかの依存関係
            var dependencies = elements.Skip(1)
                                       .Where(element => !MetaData.IsMetaDataNode(element))
                                       .Where(element => element.Name.LocalName.Contains('.'))
                                       .SelectMany(element =>
            {
                var propertyName = element.Name.LocalName.Split('.')[1];
                return element.Elements().Select(child => new Dependency(variables[element.Parent], propertyName, variables[child]));
            }).ToArray();

            return new DefinitionContent(usings, variables.Values.ToArray(), setProperties, dependencies);
        }
        #endregion

        #region GetTypeName
        /// <summary><see cref="XElement"/> から型名を取得します</summary>
        /// <param name="element">型名を取得する <see cref="XElement"/></param>
        /// <returns>型名</returns>
        private static string GetTypeName(XElement element)
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

            var genericType = MetaData.GetGenericType(element);
            var isArray = MetaData.IsArray(element);

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
        #endregion
    }
}
