using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;
using Elffy.Exceptions;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elffy.Core;

namespace Elffy.Serialization
{
    internal class SceneParser
    {
        private static readonly string[] _elffyNamespaces = new string[]
        {
            nameof(Elffy), 
            $"{nameof(Elffy)}.{nameof(UI)}",
            $"{nameof(Elffy)}.{nameof(Shape)}",
        };
        private static readonly Dictionary<string, Type> _elffyTypes;


        static SceneParser()
        {
            _elffyTypes = Assembly.GetExecutingAssembly()
                               .GetTypes()
                               .Where(t => Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) == null)
                               .Where(t => t.IsPublic && !t.IsAbstract)
                               .Where(t => _elffyNamespaces.Contains(t.Namespace))
                               .ToDictionary(t => t.Name);
        }

        #region Parse
        /// <summary>指定した <see cref="GameScene"/> を指定した隠しリソースから読み込みます</summary>
        /// <typeparam name="T">読み込む <see cref="GameScene"/> のクラス</typeparam>
        /// <param name="name">隠しリソース名</param>
        /// <returns>読み込んだ <see cref="GameScene"/></returns>
        public T Parse<T>(string name) where T : GameScene, new()
        {
            try {
                var scene = new T();
                using(var stream = Resources.GetSceneStream(name)) {
                    scene.FrameObjects = XElement.Load(stream).Elements().Select(MakeFrameObject).ToArray();
                    // TODO: シーンのイベント
                }
                return scene;
            }
            catch(SceneParseException ex) {
                throw ex;
            }
            catch(Exception ex) {
                throw new SceneParseException($"Failed in parsing Scene '{name}'.", ex);
            }
        }
        #endregion

        #region MakeFrameObject
        private FrameObject MakeFrameObject(XElement element)
        {
            var type = GetObjectType(element);
            var obj = (FrameObject)Activator.CreateInstance(type);
            var props = element.Attributes();
            foreach(var prop in props) {
                var name = prop.Name.LocalName;
                var value = prop.Value;
                var propInfo = type.GetProperty(name);
                if(propInfo == null) { continue; }
                // TODO: プロパティへの値初期化
            }
            if(element.HasElements) {
                if(obj is Positionable positionable) {
                    var children = element.Elements().Select(MakeFrameObject).Cast<Positionable>();
                    positionable.Children.AddRange(children);
                }
                else {
                    throw new SceneParseException($"'{type.FullName}' can not have children.");
                }
            }

            return obj;
        }
        #endregion

        #region GetObjetType
        private Type GetObjectType(XElement element)
        {
            var isElffyType = string.IsNullOrEmpty(element.Name.NamespaceName);
            Type type;
            if(isElffyType) {
                if(!_elffyTypes.TryGetValue(element.Name.LocalName, out type)) {
                    throw new SceneParseException($"Unknown type : '{element.Name.LocalName}'. NOTE: You forget namespace of type ?");
                }
            }
            else {
                var split = element.Name.NamespaceName.Split(';');
                var crlNamespace = split[0].Split(':')[1];
                var assembly = split.Length > 1 ? split[1].Split('=')[1] : null;
                type = Type.GetType($"{crlNamespace}.{element.Name.LocalName}");
                if(type == null) {
                    throw new SceneParseException($"Unknown type : '{crlNamespace}.{element.Name.LocalName}'");
                }
            }
            return type;
        }
        #endregion
    }
}
