using System.Collections.Immutable;
using System.Text;
using WarmLangLexerParser;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(SyntaxToken nameToken, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    :this(nameToken.Name!, parameters, type, nameToken.Location) { }


    internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, TextLocation location) 
    : base(name)
    {
        Parameters = parameters;
        Type = type;
        Location = location;
    }
    
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TypeSymbol Type { get; }

    public TextLocation Location { get; }

    public override string ToString()
    {
        var sb = new StringBuilder().Append($"{Name}(");
        for (int i = 0; i < Parameters.Length; i++)
        {
            var parm = Parameters[i];
            sb.Append(parm.Type.Name);
            if(i < Parameters.Length-1)
                sb.Append(", ");   
        }
        return sb.Append(')').ToString();
    }

    public static FunctionSymbol CreateMain()
    {
        return new FunctionSymbol(
            "wl_main",
            ImmutableArray<ParameterSymbol>.Empty,
            TypeSymbol.Void,
            new TextLocation(0,0)
        );
    }
}
