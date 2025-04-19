using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public class TypeInformation
{
    public TypeSymbol Type { get; }
    public List<MemberSymbol> Members { get; }
    public Dictionary<FunctionSymbol, BoundBlockStatement> MethodBodies { get; }
   
    public TypeInformation(TypeSymbol type, List<MemberSymbol>? members = null, 
                           Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null)  
    {
        Type = type;
        Members = members ?? new();
        MethodBodies = methodBodies ?? new();
    }

    public IEnumerable<FunctionSymbol> GetMethodFunctionSymbols()
    {
        foreach(var m in Members)
            if(m is MemberFuncSymbol mf) yield return mf.Function;
    }

    public IEnumerable<MemberFieldSymbol> GetFields()
    {
        foreach(var m in Members)
            if(m is MemberFieldSymbol mf) yield return mf;
    }
}

public class ListTypeInformation : TypeInformation
{
    public TypeSymbol SpecializedFrom { get; }
    public TypeSymbol NestedType { get; set; }

    public ListTypeInformation(TypeSymbol thisType, TypeSymbol baseT, TypeSymbol nestedType, 
                                  List<MemberSymbol>? members = null, 
                                  Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null) 
    : base(thisType, members, methodBodies)
    {
        NestedType = nestedType;
        SpecializedFrom = baseT;
    }
}

public sealed class PlaceHolderInformation : TypeInformation
{
    private static int P_COUNTER = 0;

    public int Depth { get; }
    public PlaceHolderInformation(int depth)
    : base(new TypeSymbol("P"+ P_COUNTER++))
    { 
        Depth = depth;
    }
}