using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : EntitySymbol
{
    //Function symbol contains: name, parameters, returnType
    public FunctionSymbol(SyntaxToken nameToken, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    :this(nameToken.Name!, parameters, type, nameToken.Location) { }

    internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, 
                            TypeSymbol type, TextLocation location, bool connectParams = true) 
    : base(name, type)
    {
        Parameters = parameters;
        Location = location;
        SharedLocals = new HashSet<ScopedVariableSymbol>();
        if(connectParams) foreach(var p in parameters) p.BelongsTo = this;
        //^^ TODO: object publication - could see a function symbol where the parameters aren't all updated. 
    }

    public FunctionSymbol(TypeSymbol ownerType, SyntaxToken nameToken, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    :this(nameToken.Name!, parameters, type, nameToken.Location)
    {
        OwnerType = ownerType;
    }

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
        sb.Append($"{Name}(");
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
            ImmutableArray<ParameterSymbol>.Empty,
            TypeSymbol.Void,
            new TextLocation(0,0)
        );
    
}
