using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser;

namespace WarmLangCompiler.Binding;

public class TypeInformation
{
    public TypeSymbol Type { get; }
    public List<MemberSymbol> Members { get; }
    public Dictionary<FunctionSymbol, BoundBlockStatement> MethodBodies { get; }

    [MemberNotNullWhen(true, nameof(TypeParameters))]
    public bool HasTypeParameters => TypeParameters.HasValue;
    public ImmutableArray<TypeSymbol>? TypeParameters { get; }

    public bool IsCallable => this is FunctionTypeInformation;

    public TypeInformation(TypeSymbol type, List<MemberSymbol>? members = null,
                           Dictionary<FunctionSymbol, BoundBlockStatement>? methodBodies = null,
                           ImmutableArray<TypeSymbol>? typeParameters = null)
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
                               ImmutableArray<TypeSymbol>? typeParams = null) 
    : base(thisType, baseT, new List<TypeSymbol>(){nestedType}, 0, members, typeParameters: typeParams)
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
                                  int concreteTypeArguments,
                                  List<MemberSymbol>? members = null,
                                  ImmutableArray<TypeSymbol>? typeParameters = null) 
    : base(type, members, null, typeParameters: typeParameters)
    {
        SpecializedFrom = specializedFrom;
        TypeArguments = typeArguments;
        ConcreteTypeArguments = concreteTypeArguments;
    }

    public TypeSymbol SpecializedFrom { get; }
    public List<TypeSymbol> TypeArguments { get; }
    public int ConcreteTypeArguments { get; }

    public bool IsPartiallyConcrete => TypeParameters.HasValue && ConcreteTypeArguments > 0 && ConcreteTypeArguments != TypeParameters.Value.Length;
    public bool IsFullyConcrete => TypeParameters.HasValue && ConcreteTypeArguments == TypeParameters.Value.Length;
}

public class TypeParamaterInformation : TypeInformation
{
    public TextLocation Location { get; }

    public TypeParamaterInformation(TypeSymbol tp, TextLocation location)
    : base(tp)
    {
        Location = location;
    }
}

public sealed class FunctionTypeInformation : TypeInformation
{
    public FunctionTypeInformation(TypeSymbol type,
                                   IList<TypeSymbol> parameters,
                                   TypeSymbol returnType,
                                   ImmutableArray<TypeSymbol>? typeParameters = null,
                                   bool isMemberFunc = false)
    : base(type, typeParameters: typeParameters)
    {
        Parameters = parameters;
        ReturnType = returnType;
        IsMemberFunc = isMemberFunc;
    }
    
    public bool IsMemberFunc { get; }
    public IList<TypeSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; }
}
