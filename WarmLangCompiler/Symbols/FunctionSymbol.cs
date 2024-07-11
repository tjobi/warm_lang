using System.Collections.Immutable;
using WarmLangLexerParser.AST;
using System.Text;

namespace WarmLangCompiler.Symbols;

public class FunctionSymbol : Symbol
{
    //Function symbol contains: name, parameters, returnType, body
    public FunctionSymbol(FuncDeclaration declaration, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type)
    :this(declaration.NameToken.Name!, parameters, type, declaration) { }


    internal FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol type, FuncDeclaration declaration) 
    : base(name)
    {
        Parameters = parameters;
        Type = type;
        Declaration = declaration;
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
        return sb.Append(')').ToString();
    }
}
