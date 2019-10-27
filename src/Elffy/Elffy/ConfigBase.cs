using System;
using Elffy.Serialization;

namespace Elffy
{
    /// <summary>Config基底クラス</summary>
    /// <typeparam name="T">Configクラス</typeparam>
    public abstract class ConfigBase<T> where T : ConfigBase<T>, new()
    {
        private static DataSerializer _serializer = new DataSerializer();
        private static string _path = "Config.xml";

        /// <summary>Configファイルパス</summary>
        public static string Path
        {
            get { return _path; }
            set
            {
                if(_path == null) { throw new ArgumentNullException(); }
                _path = value;
            }
        }

        /// <summary>シングルトン インスタンス</summary>
        public static T Default { get; private set; } = new T();

        /// <summary>Configを保存します</summary>
        /// <returns>保存成功したか</returns>
        public bool Save()
        {
            return _serializer.Serialize(_path, Default);
        }

        /// <summary>Configを読み込みます</summary>
        /// <returns>読み込み成功したか</returns>
        public bool Load()
        {
            T data;
            var result = _serializer.Deserialize(_path, out data);
            if(result == true) {
                Default = data;
            }
            return result;
        }
    }
}
