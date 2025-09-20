using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : EntitySymbol
{
    //Function symbol contains: name, typeParmaeters, parameters, returnType
    public FunctionSymbol(SyntaxToken nameToken,
                          ImmutableArray<TypeSymbol> typeParameters,
                          ImmutableArray<ParameterSymbol> parameters,
                          TypeSymbol type, TypeSymbol returnType,
                          TypeSymbol? ownerType = null,
                          bool isGlobal = true)
    : this(nameToken.Name!, typeParameters, parameters, type, returnType, nameToken.Location, ownerType, isGlobal: isGlobal) { }

    internal FunctionSymbol(string name,
                            ImmutableArray<TypeSymbol> typeParameters,
                            ImmutableArray<ParameterSymbol> parameters,
                            TypeSymbol functionType, TypeSymbol returnType,
                            TextLocation location,
                            TypeSymbol? ownerType = null,
                            bool connectParams = true,
                            bool isGlobal = true)
    : base(name, functionType)
    {
        TypeParameters = typeParameters;
        Parameters = parameters;
        Location = location;
        OwnerType = ownerType;
        IsGlobal = isGlobal;
        ReturnType = returnType;
        SharedLocals = new HashSet<ScopedVariableSymbol>();
        if (connectParams) foreach (var p in parameters) p.BelongsTo = this;
        //^^ TODO: object publication - could see a function symbol where the parameters aren't all updated. 
    }

    public FunctionSymbol(TypeSymbol ownerType, SyntaxToken nameToken,
                          ImmutableArray<TypeSymbol> typeParameters,
                          ImmutableArray<ParameterSymbol> parameters,
                          TypeSymbol type, TypeSymbol returnType)
    : this(nameToken.Name!, typeParameters, parameters, type, returnType, nameToken.Location)
    {
        OwnerType = ownerType;
    }

    public ImmutableArray<TypeSymbol> TypeParameters { get; }
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TextLocation Location { get; }
    public TypeSymbol ReturnType { get; }
    public TypeSymbol? OwnerType { get; }
    public bool IsGlobal { get; }

    [MemberNotNullWhen(true, nameof(OwnerType))]
    public bool IsMemberFunc => OwnerType is not null;

    //Locals that are shared with any nested functions - could be parameters or local variables
    public ISet<ScopedVariableSymbol> SharedLocals { get; set; }

    //IImmutableSet<ScopedVariableSymbol>
    public IDictionary<ScopedVariableSymbol, LocalVariableSymbol> FreeVariables { get; } = new Dictionary<ScopedVariableSymbol, LocalVariableSymbol>();

    public bool HasFreeVariables => FreeVariables.Count > 0;
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (IsMemberFunc)
        {
            sb.Append($"{OwnerType}.");
        }
        sb.Append(Name);
        if (TypeParameters.Length > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", TypeParameters));
            sb.Append('>');
        }
        return sb.Append('(').AppendJoin(", ", Parameters.Select(p => p.Type.Name)).Append(')').ToString();
    }

    //closureBelongsToThis: if true, the closure is a 1-1 match of the free variables of this function, otherwise whatever is referenced may or may not belong to this function
    public void MergeFreeVariablesWith(IDictionary<ScopedVariableSymbol, LocalVariableSymbol> closure, bool closureBelongsToThis = false)
    {
        foreach (var (v, l) in closure)
            if (!FreeVariables.ContainsKey(v) && (closureBelongsToThis || v.BelongsTo != this))
                FreeVariables[v] = closureBelongsToThis ? l : new LocalVariableSymbol(l.Name, l.Type, this);
    }
}

public static class FunctionFactory
{
    private static int lambdaID = 0;
    private static readonly TextLocation BUILT_IN_LOCATION = new(0, 0);

    public static FunctionSymbol CreateMain(string name = "wl_main")
     => new(
            name, [], [],
            TypeSymbol.Void, //TODO: fix function type
            TypeSymbol.Void,
            BUILT_IN_LOCATION
        );

    public static FunctionSymbol CreateLambda(TextLocation location, ImmutableArray<ParameterSymbol> parameters, TypeSymbol funcType, TypeSymbol returnType)
    {
        return new FunctionSymbol(
            $"__#$lambda{lambdaID++}",
            [],
            parameters,
            funcType,
            returnType,
            location,
            isGlobal: false
        );
    }

    public static FunctionSymbol CreateLocalFunction(
        SyntaxToken nameToken,
        ImmutableArray<TypeSymbol> typeParameters,
        ImmutableArray<ParameterSymbol> parameters,
        TypeSymbol funcType, TypeSymbol returnType)
    => new(nameToken, typeParameters, parameters, funcType, returnType, isGlobal: false);
    public static FunctionSymbol CreateBuiltinFunction(string name, TypeSymbol returnType, params Span<ParameterSymbol> parameters)
    => new(name, [], parameters.ToImmutableArray(), new TypeSymbol("BUILTIN:" + name), returnType, BUILT_IN_LOCATION);
}