using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using WarmLangLexerParser;
using System.Text;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(FuncDeclaration declaration, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type) : base(declaration.NameToken.Name!)
    {
        Declaration = declaration;
        Parameters = parameters;
        Type = type;
    }

    public FuncDeclaration Declaration { get; }
    public ImmutableArray<ParameterSymbol> Parameters { get; }
    public TypeSymbol Type { get; }

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
        return sb.Append(") ").ToString();
    }
}
