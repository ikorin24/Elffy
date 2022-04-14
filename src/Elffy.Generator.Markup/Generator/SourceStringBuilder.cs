#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Elffy.Generator;

public sealed class SourceStringBuilder
{
    private readonly List<MethodSourceBuilder> _methods = new List<MethodSourceBuilder>();
    private readonly StringBuilder _header = new StringBuilder();
    private readonly string _nameSpace;
    private readonly string _className;

    private int _indent;
    public int Indent { get => _indent; set => _indent = value; }

    public StringBuilder Header => _header;

    public SourceStringBuilder(string nameSpace, string className)
    {
        _nameSpace = nameSpace;
        _className = className;
    }

    public MethodSourceBuilder CreateMethodBuilder(int indent, out int num)
    {
        var methodBuilder = new MethodSourceBuilder();
        methodBuilder.Indent = indent;
        num = _methods.Count;
        _methods.Add(methodBuilder);
        return methodBuilder;
    }

    public override string ToString()
    {
        var marged = new StringBuilder();
        marged.Append(_header);
        marged.AppendLine(@$"
namespace {_nameSpace}
{{
    internal sealed class {_className}
    {{
        private {_className}()
        {{
        }}

        private struct Context : global::System.IDisposable
        {{
            private global::Elffy.Threading.ParallelOperation? _tasks;
            public void AddTask(global::Cysharp.Threading.Tasks.UniTask task)
            {{
                (_tasks ??= new global::Elffy.Threading.ParallelOperation()).Add(task);
            }}
            public global::Cysharp.Threading.Tasks.UniTask WhenAllTask()
            {{
                return _tasks?.WhenAll() ?? global::Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }}
            public void Dispose()
            {{
                _tasks?.Dispose();
            }}
        }}
");
        foreach(var mb in _methods) {
            marged.Append(mb.ToString());
        }
        marged.AppendLine(@"
    }
}");
        return marged.ToString();
    }
}

public sealed class MethodSourceBuilder
{
    private readonly StringBuilder _sb = new StringBuilder();

    private string _indentStr = "";
    private int _indent;
    public int Indent
    {
        get => _indent;
        set
        {
            _indentStr = new string(' ', value * 4);
            _indent = value;
        }
    }

    public void IncrementIndent()
    {
        Indent++;
    }

    public void DecrementIndent()
    {
        Indent = Math.Max(0, Indent - 1);
    }

    internal MethodSourceBuilder()
    {
    }

    public MethodSourceBuilder AppendLine()
    {
        _sb.AppendLine(_indentStr);
        return this;
    }

    public MethodSourceBuilder AppendLine(string str)
    {
        _sb.Append(_indentStr);
        _sb.AppendLine(str);
        return this;
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}

