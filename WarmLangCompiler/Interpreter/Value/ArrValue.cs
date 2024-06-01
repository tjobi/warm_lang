namespace WarmLangCompiler.Interpreter.Values;
using System.Text;
public sealed class ArrValue : Value
{
    public List<Value> Elements { get; }

    public ArrValue(IList<Value> elements)
    {
        Elements = elements.ToList();
    }

    public int Length => Elements.Count;

    public override string ToString()
    {
        var sb = new StringBuilder("Arr [");
        
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