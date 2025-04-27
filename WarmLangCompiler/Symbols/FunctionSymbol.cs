using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : EntitySymbol
{
    //Function symbol contains: name, parameters, returnType
    public FunctionSymbol(SyntaxToken nameToken, 
                          ImmutableArray<TypeParameterSymbol> typeParameters,
                          ImmutableArray<ParameterSymbol> parameters, 
                          TypeSymbol type)
    :this(nameToken.Name!, typeParameters, parameters, type, nameToken.Location) { }

    internal FunctionSymbol(string name, 
                            ImmutableArray<TypeParameterSymbol> typeParameters,
                            ImmutableArray<ParameterSymbol> parameters, 
                            TypeSymbol type, TextLocation location, bool connectParams = true) 
    : base(name, type)
    {
        TypeParameters = typeParameters;
        Parameters = parameters;
        Location = location;
        SharedLocals = new HashSet<ScopedVariableSymbol>();
        if(connectParams) foreach(var p in parameters) p.BelongsTo = this;
        //^^ TODO: object publication - could see a function symbol where the parameters aren't all updated. 
    }

    public FunctionSymbol(TypeSymbol ownerType, SyntaxToken nameToken, 
                          ImmutableArray<TypeParameterSymbol> typeParameters, 
                          ImmutableArray<ParameterSymbol> parameters, 
                          TypeSymbol type)
    :this(nameToken.Name!, typeParameters, parameters, type, nameToken.Location)
    {
        OwnerType = ownerType;
    }

    public ImmutableArray<TypeParameterSymbol> TypeParameters { get; }
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TextLocation Location { get; }
    public TypeSymbol? OwnerType { get; private set; }

    [MemberNotNullWhen(true, nameof(OwnerType))]
    public bool IsMemberFunc => OwnerType is not null;

    //Locals that are shared with any nested functions - could be parameters or local variables
    public ISet<ScopedVariableSymbol> SharedLocals { get; set; }

    public void SetOwnerType(TypeSymbol type) 
    {
        OwnerType ??= type;
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        if(IsMemberFunc)
        {
            sb.Append($"{OwnerType}.");
        }
        sb.Append(Name);
        if(TypeParameters.Length > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", TypeParameters));
            sb.Append('>');
        }
        return sb.Append('(').AppendJoin(", ", Parameters.Select(p => p.Type.Name)).Append(')').ToString();
    }

    public static FunctionSymbol CreateMain(string name = "wl_main")
     => new(
            name,
            ImmutableArray<TypeParameterSymbol>.Empty,
            ImmutableArray<ParameterSymbol>.Empty,
            TypeSymbol.Void,
            new TextLocation(0,0)
        );
    
}


public sealed class SpecializedFunctionSymbol : FunctionSymbol
{
    public List<TypeSymbol> TypeArguments { get; }
    public FunctionSymbol SpecializedFrom { get; }

    private static string FuncName(FunctionSymbol func, List<TypeSymbol> typeParams)
    {
        var sb = new StringBuilder(func.Name);
        sb.Append('<');
        sb.AppendJoin(", ", typeParams);
        sb.Append('>');
        return sb.ToString();
    }
    public SpecializedFunctionSymbol(FunctionSymbol func, List<TypeSymbol> typeArguments,
                                     ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType,
                                     TextLocation location) 
    : base(FuncName(func, typeArguments), func.TypeParameters, 
           parameters, returnType, location)
    {
        TypeArguments = typeArguments;
        SpecializedFrom = func;
    }

    public override string ToString()
    {
        return Name;
    }
}
