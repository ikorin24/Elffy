#nullable enable
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using Elffy.Exceptions;

namespace Elffy.Serialization
{
    internal class FbxParser         // TODO: ガベージの考慮が全くされてないのでもっと減らせる
    {
        #region private member
        /// <summary>Magic Word of Binary FBX</summary>
        private static byte[] MAGIC_WORD = new byte[23]
        {
            0x4B, 0x61, 0x79, 0x64, 0x61, 0x72, 0x61, 0x20,
            0x46, 0x42, 0x58, 0x20, 0x42, 0x69, 0x6E, 0x61,
            0x72, 0x79, 0x20, 0x20, 0x00, 0x1a, 0x00,
        };

        private const byte BOOL_PROPERTY = 0x43;      // 'C'
        private const byte INT16_PROPERTY = 0x59;     // 'Y'
        private const byte INT32_PROPERTY = 0x49;     // 'I'
        private const byte FLOAT_PROPERTY = 0x46;     // 'F'
        private const byte DOUBLE_PROPERTY = 0x44;    // 'D'
        private const byte INT64_PROPERTY = 0x4C;     // 'L'

        private const byte BOOL_ARRAY_PROPERTY = 0x62;    // 'b'
        private const byte INT32_ARRAY_PROPERTY = 0x69;   // 'i'
        private const byte FLOAT_ARRAY_PROPERTY = 0x66;   // 'f'
        private const byte DOUBLE_ARRAY_PROPERTY = 0x64;  // 'd'
        private const byte INT64_ARRAY_PROPERTY = 0x6c;   // 'l'

        private const byte STRING_PROPERTY = 0x53;        // 'S'
        private const byte RAW_BINARY_PROPERTY = 0x52;    // 'R'
        #endregion

        #region Parse
        /// <summary>Parse fbx file</summary>
        /// <param name="filepath">fbx file name</param>
        /// <returns>fbx object</returns>
        public FbxObject Parse(string filepath)
        {
            ArgumentChecker.ThrowIfNullArg(filepath, nameof(filepath));
            ArgumentChecker.ThrowFileNotFoundIf(!File.Exists(filepath), "file not found", filepath);
            using (var stream = File.OpenRead(filepath))
            using (var reader = new BinaryReader(stream))
            {
                var fbxObj = new FbxObject();
                ParsePrivate(reader, fbxObj);
                return fbxObj;
            }
        }

        /// <summary>Parse fbx file from specified stream</summary>
        /// <param name="stream">stream of fbx file</param>
        /// <returns>fbx object</returns>
        public FbxObject Parse(Stream stream)
        {
            ArgumentChecker.ThrowIfNullArg(stream, nameof(stream));
            using (var reader = new BinaryReader(stream))
            {
                var fbxObj = new FbxObject();
                ParsePrivate(reader, fbxObj);
                return fbxObj;
            }
        }
        #endregion

        #region private Method
        /// <summary>Parse entire fbx</summary>
        /// <param name="reader">Binary Reader</param>
        /// <param name="fbxObj">fbx object</param>
        private void ParsePrivate(BinaryReader reader, FbxObject fbxObj)
        {
            ParseHeader(reader, fbxObj);
            while (true)
            {
                if (!ParseNodeRecord(reader, fbxObj, out var node)) { break; }
                fbxObj.Children.Add(node!);
            }
            ParseFooter(reader, fbxObj);
        }

        /// <summary>Parse FBX Header</summary>
        /// <param name="reader">Binary Reader</param>
        /// <param name="fbxObj">fbx object</param>
        private void ParseHeader(BinaryReader reader, FbxObject fbxObj)
        {
            // read magic word, and check it
            var magicWord = reader.ReadBytes(MAGIC_WORD.Length);
            var valid = magicWord.Zip(MAGIC_WORD, (a, b) => a == b).All(x => x);
            if (!valid) { throw new FormatException(); }

            // read version
            var version = (int)reader.ReadUInt32();
            fbxObj.FormatVersion = version;
        }

        /// <summary>Parse Node Record. (Parse child node recursively)</summary>
        /// <param name="reader">Binary Reader</param>
        /// <param name="root">root node</param>
        /// <param name="node">current node</param>
        private bool ParseNodeRecord(BinaryReader reader, FbxObject root, out FbxNode? node)
        {
            // read node infomation
            ulong endOfRecord;
            ulong propertyCount;
            ulong propertyListLen;
            byte nameLen;
            if (root.FormatVersion >= 7400 || root.FormatVersion < 7500)
            {
                endOfRecord = reader.ReadUInt32();
                propertyCount = reader.ReadUInt32();
                propertyListLen = reader.ReadUInt32();
                nameLen = reader.ReadByte();
            }
            else if (root.FormatVersion >= 7500)
            {
                endOfRecord = reader.ReadUInt64();
                propertyCount = reader.ReadUInt64();
                propertyListLen = reader.ReadUInt64();
                nameLen = reader.ReadByte();
            }
            else
            {
                throw new FormatException();
            }
            var isNullRecord = (endOfRecord == 0) && (propertyCount == 0) && (propertyListLen == 0) && (nameLen == 0);
            if (isNullRecord)
            {
                node = null;
                return false;
            }

            node = new FbxNode();
            node.Name = GetAsciiString(reader.ReadBytes(nameLen));

            // read properties
            for (ulong i = 0; i < propertyCount; i++)
            {
                ParseProperty(reader, node);
            }

            // read child node
            var hasChildren = (ulong)reader.BaseStream.Position != endOfRecord;
            var hasNullRecord = hasChildren || propertyCount == 0;
            if (hasChildren || hasNullRecord)
            {
                while (true)
                {
                    if (!ParseNodeRecord(reader, root, out var child)) { break; }
                    node.Children.Add(child!);
                }
            }
            return true;
        }

        /// <summary>Parse Node Property</summary>
        /// <param name="reader">Binary Reader</param>
        /// <param name="node">current node</param>
        private void ParseProperty(BinaryReader reader, FbxNode node)
        {
            #region local func GetArrayProperty
            byte[] GetArrayProperty(BinaryReader r, int typeSize)
            {
                var len = (int)r.ReadUInt32();
                var encoded = r.ReadUInt32() != 0;
                var compressedSize = (int)r.ReadUInt32();
                if (encoded)
                {
                    var deflateMetaData = r.ReadInt16();
                    const int deflateMetaDataSize = 2;
                    var byteArray = r.ReadBytes(compressedSize - deflateMetaDataSize);
                    using (var ms = new MemoryStream(byteArray))
                    using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        try
                        {
                            var decompressed = new byte[len * typeSize];
                            if (ds.Read(decompressed, 0, decompressed.Length) != decompressed.Length) { throw new FormatException(); }
                            return decompressed;
                        }
                        catch (InvalidDataException ex)
                        {
                            throw new FormatException("Parse fail", ex);
                        }
                    }
                }
                else
                {
                    var byteArray = r.ReadBytes(compressedSize);
                    return byteArray;
                }
            }
            #endregion

            var propertyType = reader.ReadByte();
            switch (propertyType)
            {
                case INT16_PROPERTY:
                {
                    var value = reader.ReadInt16();
                    node.Properties.Add(new FbxShortProperty() { Value = value });
                    break;
                }
                case BOOL_PROPERTY:
                {
                    var tmp = reader.ReadByte();
                    // two types format exists. (Oh my gosh !! Fuuuuuuuu*k !!)
                    // blender             -> true/false = 0x01/0x00
                    // Autodesk production -> true/false = 'Y'/'T' = 0x59/0x54
                    var value = (tmp == 0x00 || tmp == 0x54) ? false : true;
                    node.Properties.Add(new FbxBoolProperty() { Value = value });
                    break;
                }
                case INT32_PROPERTY:
                {
                    var value = reader.ReadInt32();
                    node.Properties.Add(new FbxIntProperty() { Value = value });
                    break;
                }
                case FLOAT_PROPERTY:
                {
                    var value = reader.ReadSingle();
                    node.Properties.Add(new FbxFloatProperty() { Value = value });
                    break;
                }
                case DOUBLE_PROPERTY:
                {
                    var value = reader.ReadDouble();
                    node.Properties.Add(new FbxDoubleProperty() { Value = value });
                    break;
                }
                case INT64_PROPERTY:
                {
                    var value = reader.ReadInt64();
                    node.Properties.Add(new FbxLongProperty() { Value = value });
                    break;
                }
                case STRING_PROPERTY:
                {
                    var len = (int)reader.ReadUInt32();
                    var value = GetAsciiString(reader.ReadBytes(len));
                    node.Properties.Add(new FbxStringProperty() { Value = value });
                    break;
                }

                case BOOL_ARRAY_PROPERTY:
                {
                    var byteArray = GetArrayProperty(reader, 1);
                    var prop = new FbxBoolArrayProperty() { Value = GetBoolArray(byteArray) };
                    node.Properties.Add(prop);
                    break;
                }
                case INT32_ARRAY_PROPERTY:
                {
                    var byteArray = GetArrayProperty(reader, 4);
                    var prop = new FbxIntArrayProperty() { Value = GetIntArray(byteArray) };
                    node.Properties.Add(prop);
                    break;
                }
                case FLOAT_ARRAY_PROPERTY:
                {
                    var byteArray = GetArrayProperty(reader, 4);
                    var prop = new FbxFloatArrayProperty() { Value = GetFloatArray(byteArray) };
                    node.Properties.Add(prop);
                    break;
                }
                case DOUBLE_ARRAY_PROPERTY:
                {
                    var byteArray = GetArrayProperty(reader, 8);
                    var prop = new FbxDoubleArrayProperty() { Value = GetDoubleArray(byteArray) };
                    node.Properties.Add(prop);
                    break;
                }
                case INT64_ARRAY_PROPERTY:
                {
                    var byteArray = GetArrayProperty(reader, 8);
                    var prop = new FbxLongArrayProperty() { Value = GetLongArray(byteArray) };
                    node.Properties.Add(prop);
                    break;
                }
                case RAW_BINARY_PROPERTY:
                {
                    var len = (int)reader.ReadUInt32();
                    var value = reader.ReadBytes(len);
                    var prop = new FbxBinaryProperty() { Value = value };
                    node.Properties.Add(prop);
                    break;
                }

                default:
                {
                    Debug.WriteLine($"[Skip Unknow Type Property] Position : {reader.BaseStream.Position}, type : {propertyType}");
                    break;
                }
            }
        }

        private void ParseFooter(BinaryReader reader, FbxObject fbxObj)
        {

        }

        private bool[] GetBoolArray(byte[] array)
        {
            return array.Select(b => b != 0).ToArray();
        }

        private int[] GetIntArray(byte[] array)
        {
            const int size = 4;
            if (array.Length % size != 0) { throw new FormatException(); }
            var ret = new int[array.Length / size];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = BitConverter.ToInt32(array, i * size);
            }
            return ret;
        }

        private long[] GetLongArray(byte[] array)
        {
            const int size = 8;
            if (array.Length % size != 0) { throw new FormatException(); }
            var ret = new long[array.Length / size];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = BitConverter.ToInt64(array, i * size);
            }
            return ret;
        }

        private float[] GetFloatArray(byte[] array)
        {
            const int size = 4;
            if (array.Length % size != 0) { throw new FormatException(); }
            var ret = new float[array.Length / size];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = BitConverter.ToSingle(array, i * size);
            }
            return ret;
        }

        private double[] GetDoubleArray(byte[] array)
        {
            const int size = 8;
            if (array.Length % size != 0) { throw new FormatException(); }
            var ret = new double[array.Length / size];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = BitConverter.ToDouble(array, i * size);
            }
            return ret;
        }

        private string GetAsciiString(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x00 || bytes[i] == 0x01)
                {
                    bytes[i] = 0x3a;    // replace (0x00, 0x01) into 0x3a ':'
                }
            }
            return Encoding.ASCII.GetString(bytes);
        }
        #endregion private Method
    }
}
