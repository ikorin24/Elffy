#nullable enable
using Elffy.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Elffy.Serialization
{
    /// <summary>Xmlシリアライザクラス</summary>
    internal class DataSerializer
    {
        protected Encoding _encoding = Encoding.UTF8;

        /// <summary>xmlシリアライズを実行します</summary>
        /// <typeparam name="T">対象の型</typeparam>
        /// <param name="path">ファイルパス</param>
        /// <param name="data">シリアライズ対象のオブジェクト</param>
        public void Serialize<T>(string path, T data)
        {
            ArgumentChecker.ThrowIfNullArg(path, nameof(path));
            var ws = new XmlWriterSettings();
            ws.Encoding = _encoding;
            ws.Indent = true;
            ws.IndentChars = "  ";
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var dir = Path.GetDirectoryName(path)!;
            var tmpfile = Path.Combine(dir, "___tmp___file___");
            if(dir != "") {
                Directory.CreateDirectory(dir);
            }
            try {
                if(File.Exists(path)) {
                    File.Move(path, tmpfile);
                }
                using(var stream = File.Open(path, FileMode.OpenOrCreate))
                using(var writer = XmlWriter.Create(stream, ws)) {
                    var serializer = new XmlSerializer(typeof(T), typeof(T).GetNestedTypes());
                    serializer.Serialize(writer, data, ns);
                    if(File.Exists(tmpfile)) {
                        File.Delete(tmpfile);
                    }
                }
            }
            catch(Exception ex) {
                if(File.Exists(path)) {
                    File.Delete(path);
                }
                if(File.Exists(tmpfile)) {
                    File.Move(tmpfile, path);
                }
                throw ex;
            }
        }

        /// <summary>xmlデシリアライズを実行します</summary>
        /// <typeparam name="T">対象の型</typeparam>
        /// <param name="path">ファイルパス</param>
        /// <returns>デシリアライズしたデータ</returns>
        public T Deserialize<T>(string path)
        {
            if(!File.Exists(path)) {
                throw new FileNotFoundException(path);
            }
            using(var stream = File.OpenRead(path)) {
                return Deserialize<T>(stream);
            }
        }

        /// <summary>xmlデシリアライズを実行します</summary>
        /// <typeparam name="T">対象の型</typeparam>
        /// <param name="stream">ストリーム</param>
        /// <returns>デシリアライズしたデータ</returns>
        public T Deserialize<T>(Stream stream)
        {
            try {
                using(var reader = XmlReader.Create(stream)) {
                    var serializer = new XmlSerializer(typeof(T), typeof(T).GetNestedTypes());
                    var data = (T)serializer.Deserialize(reader);
                    return data;
                }
            }
            catch(Exception ex) {
                throw new InvalidDataException("Deserializing failure.", ex);
            }
        }
    }
}
