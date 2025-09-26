using WarmLangCompiler.Binding.BoundAccessing;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundFuncTypeApplication : BoundExpression
{
    public BoundFuncTypeApplication(ExpressionNode node, BoundAccess access, SpecializedFunctionSymbol funcSymbol) 
    : base(node, funcSymbol.Type)
    {
        Access = access;
        Specialized = funcSymbol;
    }

    public BoundAccess Access { get; }
    public SpecializedFunctionSymbol Specialized { get; }
}