using System.Text;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public class UnionFind
{
    private Dictionary<int, int> _parent;
    private readonly Func<TypeSymbol, int> toInt;

    public UnionFind(Func<TypeSymbol, int> toInt)
    {
        _parent = new();
        this.toInt = toInt;
    }

    public void Add(int i, int parent) => _parent[i] = parent;
    public void Add(TypeSymbol a, TypeSymbol p) => _parent[toInt(a)] = toInt(p);
    public void Add(TypeSymbol t) => Add(t,t);

    public int Find(TypeSymbol i) => Find(toInt(i));

    public int Find(int i) 
    {
        int root = _parent[i];

        if(_parent[root] != root) {
            return _parent[i] = Find(root);
        }
        return root;
    }

    public bool TryFind(TypeSymbol t, out int result) =>  TryFind(toInt(t), out result);

    public bool TryFind(int i, out int result)
    {
        result = 0;
        if(!_parent.TryGetValue(i, out var root)) return false;
        result = root;

        if(_parent[root] != root) {
            if(TryFind(root, out var parRoot))
                result = _parent[i] = parRoot;
        }
        return true;
    }

    public void Union(TypeSymbol a, TypeSymbol b) => Union(toInt(a), toInt(b));
    public void Union(int i, int j)
    {
        int ip = Find(i);
        int jp = Find(j);
        _parent[ip] = jp;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Union\n  ");
        sb.AppendJoin("\n  ", _parent.Select((t) => $"{t.Key} => {t.Value}"));
        return sb.ToString();
    }
}