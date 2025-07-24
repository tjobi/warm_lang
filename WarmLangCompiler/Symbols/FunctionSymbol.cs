using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using WarmLangCompiler.Binding;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : EntitySymbol
{
    //Function symbol contains: name, parameters, returnType
    public FunctionSymbol(SyntaxToken nameToken,
                          ImmutableArray<TypeSymbol> typeParameters,
                          ImmutableArray<ParameterSymbol> parameters,
                          TypeSymbol type, TypeSymbol returnType)
    : this(nameToken.Name!, typeParameters, parameters, type, returnType, nameToken.Location) { }

    internal FunctionSymbol(string name,
                            ImmutableArray<TypeSymbol> typeParameters,
                            ImmutableArray<ParameterSymbol> parameters,
                            TypeSymbol functionType, TypeSymbol returnType,
                            TextLocation location,
                            bool connectParams = true)
    : base(name, functionType)
    {
        TypeParameters = typeParameters;
        Parameters = parameters;
        Location = location;
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

    public static FunctionSymbol CreateMain(string name = "wl_main")
     => new(
            name,
            ImmutableArray<TypeSymbol>.Empty,
            ImmutableArray<ParameterSymbol>.Empty,
            TypeSymbol.Void, //TODO: fix function type
            TypeSymbol.Void,
            new TextLocation(0, 0)
        );

}

public sealed class LambdaFunctionSymbol : FunctionSymbol
{
    //TODO: Remember to change EntitySymbol.Type to not have a setter!
    public LambdaFunctionSymbol(TextLocation location, ImmutableArray<ParameterSymbol> parameters, TypeSymbol funcType, TypeSymbol returnType)
    : base($"__#$lambda_at_{location}", ImmutableArray<TypeSymbol>.Empty, parameters, funcType, returnType, location)
    { }

    [MemberNotNullWhen(true, nameof(Body))]
    public bool IsComplete => Body is not null;
    public BoundBlockStatement? Body { get; private set; } = null;

    public void SetBody(BoundBlockStatement body) => Body = body;
}