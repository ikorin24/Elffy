using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Serialization
{
    /// <summary>A fbx objsct, which is root node of hierarchical tree</summary>
    public class FbxObject : FbxNode
    {
        /// <summary>format version of fbx file</summary>
        internal int FormatVersion { get; set; }

        /// <summary>constructor of <see cref="FbxObject"/></summary>
        public FbxObject()
        {
            Name = "Root";
        }
    }

    /// <summary>A fbx node in hierarchical tree</summary>
    [DebuggerDisplay("{Name}")]
    public class FbxNode
    {
        /// <summary>get wheter this node has children node</summary>
        public bool HasChildren => Children.Count > 0;
        /// <summary>get wheter this node has properties</summary>
        public bool HasProperties => Properties.Count > 0;
        /// <summary>get children nodes of this node</summary>
        public List<FbxNode> Children { get; }
        /// <summary>get properties of this node</summary>
        public List<FbxProperty> Properties { get; }
        /// <summary>get of set name of this node</summary>
        public string Name { get; set; }

        /// <summary>constructor of <see cref="FbxNode"/></summary>
        public FbxNode()
        {
            Children = new List<FbxNode>();
            Properties = new List<FbxProperty>();
        }

        /// <summary>Dump summary of hierarchical tree whose root is this node</summary>
        /// <param name="showProp">show properties infomation of each nodes. (default: true)</param>
        /// <returns>hierarchical tree string</returns>
        public virtual string Dump(bool showProp = true)
        {
            return DumpWithIndent(0, showProp);
        }

        public virtual string DumpJson()
        {
            return Json(0);
        }

        protected string Json(int nest)
        {
            const int indentWidth = 2;
            var indent = new string(' ', indentWidth * nest);
            var indent2 = new string(' ', indentWidth * (nest + 1));
            var sb = new StringBuilder();
            var firstItem = true;
            sb.Append("{");
            for (int i = 0; i < Properties.Count; i++)
            {
                if (firstItem)
                {
                    sb.AppendLine();
                    firstItem = false;
                }
                else
                {
                    sb.AppendLine(",");
                }
                sb.AppendFormat("{0}\"[{1}]\" : {2}", indent2, i, Properties[i].DumpJson());
            }
            foreach (var child in Children)
            {
                if (firstItem)
                {
                    sb.AppendLine();
                    firstItem = false;
                }
                else
                {
                    sb.AppendLine(",");
                }
                sb.AppendFormat("{0}\"{1}\" : {2}", indent2, child.Name, child.Json(nest + 1));
            }
            sb.AppendLine();
            sb.AppendFormat("{0}{1}", indent, "}");
            return sb.ToString();
        }

        /// <summary>Dump info with indent</summary>
        /// <param name="nest">nest count</param>
        /// <param name="showProp">show properties</param>
        /// <returns>hierarchical tree string</returns>
        protected string DumpWithIndent(int nest, bool showProp)
        {
            const int indentWidth = 4;
            var indent = new string(' ', indentWidth * nest);
            var indent2 = new string(' ', indentWidth * (nest + 1));
            var sb = new StringBuilder();
            sb.AppendFormat("{0}{1} (Properties={2}, Children={3})", indent, Name, Properties.Count, Children.Count);
            if (showProp)
            {
                for (int i = 0; i < Properties.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendFormat("{0}[{1}] {2}", indent2, i, Properties[i].Dump());
                }
            }
            foreach (var child in Children)
            {
                sb.AppendLine();
                sb.Append(child.DumpWithIndent(nest + 1, showProp));
            }
            return sb.ToString();
        }
    }

    /// <summary>A property infomation of <see cref="FbxNode"/></summary>
    public abstract class FbxProperty
    {
        /// <summary>get type of this property. (if array property, type of an item in array.)</summary>
        public Type ValueType { get; protected set; }
        /// <summary>get wheter this <see cref="FbxProperty"/> is array type.</summary>
        public bool IsArray { get; protected set; }

        /// <summary>get value which this <see cref="FbxProperty"/> has.</summary>
        /// <returns>value of this <see cref="FbxProperty"/></returns>
        public abstract object GetValue();

        /// <summary>Dump summary of this <see cref="FbxProperty"/></summary>
        /// <returns>summary string</returns>
        public abstract string Dump();

        public abstract string DumpJson();
    }

    /// <summary>A not-array property infomation of <see cref="FbxNode"/></summary>
    /// <typeparam name="T">type of property</typeparam>
    public abstract class FbxValueProperty<T> : FbxProperty
    {
        /// <summary>get of set property value</summary>
        public T Value { get; set; }

        /// <summary>constructor of <see cref="FbxValueProperty{T}"/></summary>
        public FbxValueProperty()
        {
            ValueType = typeof(T);
            IsArray = false;
        }

        /// <summary>get value which this <see cref="FbxProperty"/> has.</summary>
        /// <returns>value of this <see cref="FbxProperty"/></returns>
        public override object GetValue() => Value as object;
    }

    /// <summary>An array property infomation of <see cref="FbxNode"/></summary>
    /// <typeparam name="T">type of an item in array</typeparam>
    public abstract class FbxArrayProperty<T> : FbxProperty
    {
        /// <summary>get of set property value</summary>
        public T[] Value { get; set; }

        /// <summary>constructor of <see cref="FbxArrayProperty{T}"/></summary>
        public FbxArrayProperty()
        {
            ValueType = typeof(T[]);
            IsArray = true;
        }

        /// <summary>get value which this <see cref="FbxProperty"/> has.</summary>
        /// <returns>value of this <see cref="FbxProperty"/></returns>
        public override object GetValue() => Value as object;
    }

    /// <summary>an <see cref="int"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("int : {Value}")]
    public sealed class FbxIntProperty : FbxValueProperty<int>
    {
        /// <summary>Dump summary of this <see cref="FbxIntProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"int : {Value}";

        public override string DumpJson() => Value.ToString();
    }

    /// <summary>a <see cref="short"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("short : {Value}")]
    public sealed class FbxShortProperty : FbxValueProperty<short>
    {
        /// <summary>Dump summary of this <see cref="FbxShortProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"short : {Value}";

        public override string DumpJson() => Value.ToString();
    }

    /// <summary>a <see cref="long"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("long : {Value}")]
    public sealed class FbxLongProperty : FbxValueProperty<long>
    {
        /// <summary>Dump summary of this <see cref="FbxLongProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"long : {Value}";

        public override string DumpJson() => Value.ToString();
    }

    /// <summary>a <see cref="float"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("float : {Value}")]
    public sealed class FbxFloatProperty : FbxValueProperty<float>
    {
        /// <summary>Dump summary of this <see cref="FbxFloatProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"float : {Value}";

        public override string DumpJson() => Value.ToString();
    }

    /// <summary>a <see cref="double"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("double : {Value}")]
    public sealed class FbxDoubleProperty : FbxValueProperty<double>
    {
        /// <summary>Dump summary of this <see cref="FbxDoubleProperty"/></summary>
        /// <returns>summary string</returns>a
        public override string Dump() => $"double : {Value}";

        public override string DumpJson() => Value.ToString();
    }

    /// <summary>a <see cref="bool"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("bool : {Value}")]
    public sealed class FbxBoolProperty : FbxValueProperty<bool>
    {
        /// <summary>Dump summary of this <see cref="FbxBoolProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"bool : {Value}";

        public override string DumpJson() => Value ? "true" : "false";
    }

    /// <summary>a <see cref="string"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("string : {Value}")]
    public sealed class FbxStringProperty : FbxValueProperty<string>
    {
        /// <summary>Dump summary of this <see cref="FbxStringProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"string : {Value}";

        public override string DumpJson() => $"\"{Value}\"";
    }

    /// <summary>an array of <see cref="int"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("int[]")]
    public sealed class FbxIntArrayProperty : FbxArrayProperty<int>
    {
        /// <summary>Dump summary of this <see cref="FbxIntArrayProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"int[] : {string.Join(", ", Value)}";

        public override string DumpJson() => $"[{string.Join(",", Value)}]";
    }

    /// <summary>an array of <see cref="long"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("long[]")]
    public sealed class FbxLongArrayProperty : FbxArrayProperty<long>
    {
        /// <summary>Dump summary of this <see cref="FbxLongArrayProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"long[] : {string.Join(", ", Value)}";

        public override string DumpJson() => $"[{string.Join(",", Value)}]";
    }

    /// <summary>an array of <see cref="float"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("float[]")]
    public sealed class FbxFloatArrayProperty : FbxArrayProperty<float>
    {
        /// <summary>Dump summary of this <see cref="FbxFloatArrayProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"float[] : {string.Join(", ", Value)}";

        public override string DumpJson() => $"[{string.Join(",", Value)}]";
    }

    /// <summary>an array of <see cref="double"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("double[]")]
    public sealed class FbxDoubleArrayProperty : FbxArrayProperty<double>
    {
        /// <summary>Dump summary of this <see cref="FbxDoubleArrayProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"double[] : {string.Join(", ", Value)}";

        public override string DumpJson() => $"[{string.Join(",", Value)}]";
    }

    /// <summary>an array of <see cref="bool"/> property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("bool[]")]
    public sealed class FbxBoolArrayProperty : FbxArrayProperty<bool>
    {
        /// <summary>Dump summary of this <see cref="FbxBoolArrayProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump() => $"bool[] : {string.Join(", ", Value)}";

        public override string DumpJson() => $"[{string.Join(",", Value)}]";
    }

    /// <summary>a raw binary property of <see cref="FbxNode"/></summary>
    [DebuggerDisplay("[Raw-Binary]")]
    public sealed class FbxBinaryProperty : FbxArrayProperty<byte>
    {
        /// <summary>Dump summary of this <see cref="FbxBinaryProperty"/></summary>
        /// <returns>summary string</returns>
        public override string Dump()
        {
            var hex = Value.Select(b => $"0x{b.ToString("x2")}");
            return $"raw bin : {string.Join(", ", hex)}";
        }

        public override string DumpJson() => $"[{string.Join(",", Value.Select(b => $"\"0x{b.ToString("x2")}\""))}]";
    }
}
