using System.Collections.Immutable;
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
        sb.Append('(');
        for (int i = 0; i < Parameters.Length; i++)
        {
            var parm = Parameters[i];
            sb.Append(parm.Type.Name);
            if(i < Parameters.Length-1)
                sb.Append(", ");   
        }
        return sb.Append(')').ToString();
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
    public new List<TypeSymbol> TypeParameters { get; }
    public FunctionSymbol SpecializedFrom { get; }

    private static string FuncName(FunctionSymbol func, List<TypeSymbol> typeParams)
    {
        var sb = new StringBuilder(func.Name);
        sb.Append('<');
        sb.AppendJoin(", ", typeParams);
        sb.Append('>');
        return sb.ToString();
    }
    public SpecializedFunctionSymbol(FunctionSymbol func, List<TypeSymbol> typeParams) 
    : base(FuncName(func, typeParams), func.TypeParameters, 
           func.Parameters, func.Type, func.Location, 
           connectParams:false)
    {
        TypeParameters = typeParams;
        SpecializedFrom = func;
    }

    public override string ToString()
    {
        return Name;
    }
}
