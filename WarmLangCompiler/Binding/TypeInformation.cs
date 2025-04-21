using System.Collections.Immutable;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public class TypeInformation
{
    public TypeSymbol Type { get; }
    public List<MemberSymbol> Members { get; }
    public Dictionary<FunctionSymbol, BoundBlockStatement> MethodBodies { get; }
    public ImmutableArray<TypeParameterSymbol>? TypeParameters { get; }

    public TypeInformation(TypeSymbol type, List<MemberSymbol>? members = null, 
                           Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null,
                           ImmutableArray<TypeParameterSymbol>? typeParameters = null)  
    {
        Type = type;
        Members = members ?? new();
        MethodBodies = methodBodies ?? new();
        TypeParameters = typeParameters;
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

public class ListTypeInformation : GenericTypeInformation
{
    public TypeSymbol NestedType => TypeArguments[0];

    public ListTypeInformation(TypeSymbol thisType, TypeSymbol baseT, TypeSymbol nestedType, 
                                  List<MemberSymbol>? members = null, 
                                  Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null) 
    : base(thisType, baseT, new List<TypeSymbol>(){nestedType}, members, methodBodies)
    { }
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

public class GenericTypeInformation : TypeInformation
{
    public GenericTypeInformation(TypeSymbol type, TypeSymbol specializedFrom, List<TypeSymbol> typeArguments, 
                                  List<MemberSymbol>? members = null, 
                                  Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null) : base(type, members, methodBodies)
    {
        SpecializedFrom = specializedFrom;
        TypeArguments = typeArguments;
    }

    public TypeSymbol SpecializedFrom { get; }
    public List<TypeSymbol> TypeArguments { get; }
}
