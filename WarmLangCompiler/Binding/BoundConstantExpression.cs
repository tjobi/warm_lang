namespace WarmLangCompiler.Binding;

using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

public class BoundConstantExpression : BoundExpression
{
    public BoundConstant Constant { get; }
    public BoundConstantExpression(ConstExpression ce, TypeSymbol type)
        : base(ce, type)
    { 
        Constant = new BoundConstant(ce.Value);
    }
}