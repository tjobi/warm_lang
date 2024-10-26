using System.Diagnostics.CodeAnalysis;
using System.Text;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Interpreter.Values;

using FieldDict = Dictionary<string, Value>;

public sealed record StructValue : Value
{
    public StructValue(TypeSymbol type) : this(type, new()) { }
    public StructValue(TypeSymbol type, FieldDict fields)
    {
        Name = type.Name;
        Fields = fields;
    }

    public string Name { get; }
    public FieldDict Fields { get; }

    public Value this[string idx]
    {
        get => Fields[idx];
        set => Fields[idx] = value;
    }

    public bool TryGetField(string name, [NotNullWhen(true)] out Value? res) => Fields.TryGetValue(name, out res);

    public void AddField(string name, Value val)
    {
        if(!Fields.ContainsKey(name)) Fields[name] = val;
    }

    public bool IsFieldInitialized(string name) => Fields.ContainsKey(name);

    public override string StdWriteString()
    {
        var sb = new StringBuilder().Append(Name).Append(" { ");
        var cnt = 0;
        foreach(var f in Fields) 
        {
            sb.Append(f.Key).Append(" = ").Append(f.Value.StdWriteString());
            if(cnt++ < Fields.Count-1) sb.Append(", ");
        }
        return sb.Append(" }").ToString();
    }

    public override string ToString() => StdWriteString();
}