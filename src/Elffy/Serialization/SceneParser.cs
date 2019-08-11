using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace Elffy.Serialization
{
    internal class SceneParser
    {
        #region Parse
        /// <summary>指定した <see cref="GameScene"/> を指定した隠しリソースから読み込みます</summary>
        /// <typeparam name="T">読み込む <see cref="GameScene"/> のクラス</typeparam>
        /// <param name="name">隠しリソース名</param>
        /// <returns>読み込んだ <see cref="GameScene"/></returns>
        public T Parse<T>(string name) where T : GameScene, new()
        {
            var scene = new T();
            using(var stream = Resources.LoadHiddenStream(name)) {
                scene.FrameObjects = XElement.Load(stream).Elements().Select(MakeFrameObject).ToArray();
                // TODO: シーンのイベント
            }
            return scene;
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
            return obj;
        }
        #endregion

        #region GetObjetType
        private Type GetObjectType(XElement element)
        {
            var split = element.Name.NamespaceName.Split(';');
            var crlNamespace = split[0].Split(':')[1];
            var assembly = split.Length > 1 ? split[1].Split('=')[1] : null;
            var type = Type.GetType($"{crlNamespace}.{element.Name.LocalName}");
            return type;
        }
        #endregion
    }
}
