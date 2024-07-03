namespace WarmLangCompiler.Interpreter.Values;
using System.Text;
public sealed record class ListValue : Value
{
    public List<Value> Elements { get; }

    public ListValue(IList<Value> elements)
    {
        Elements = elements.ToList();
    }

    public ListValue(int initCapacity)
    {
        Elements = new List<Value>(initCapacity);
    }

    public int Length => Elements.Count;

    public Value this[int idx]
    {
        get
        {
            return Elements[idx];
        }

        set
        {
            Elements[idx] = value;
        }
    }

    public ListValue Add(Value v)
    {
        Elements.Add(v);
        return this;
    }

    public Value RemoveLast()
    {
        if(Elements.Count == 0)
        {
            throw new Exception("Cannot remove elements from an empty array");
        }
        var last = Elements[^1];
        Elements.RemoveAt(Length-1);
        return last;
    }

    public bool IsEqualTo(ListValue b) => DeepEquality(this, b); 

    public static bool DeepEquality(ListValue a, ListValue b)
    {
        if(a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if(a[i] != b[i])
                return false;
        }
        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("List [");
        
        for(int i = 0; i < Elements.Count; i++)
        {
            var val = Elements[i];
            sb.Append(val.ToString());
            if(i < Elements.Count-1)
            {
                sb.Append(", ");
            }
        }
        return sb.Append(']').ToString();
    }
}