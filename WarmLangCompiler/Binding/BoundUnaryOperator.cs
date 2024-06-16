using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangCompiler.Binding;

public sealed class BoundUnaryOperator
{
    public BoundUnaryOperator(TokenKind op, TypeSymbol left, TypeSymbol resultType)
    {
        Operator = op;
        Type = resultType;
        TypeLeft = left;
    }

    public TokenKind Operator { get; }
    public TypeSymbol TypeLeft { get; }
    public TypeSymbol Type { get; }

    public static BoundUnaryOperator? Bind(TokenKind op, BoundExpression left)
    {
        foreach (var dop in _definedOperators)
        {
            if(dop.Operator == op && left.Type == dop.TypeLeft)
            {
                return dop;
            }
        }
        return null;
    }

    private static BoundUnaryOperator[] _definedOperators = new BoundUnaryOperator[]
    {
        new(TPlus, TypeSymbol.Int, TypeSymbol.Int),
        new(TMinus, TypeSymbol.Int, TypeSymbol.Int),
        new(TLeftArrow, TypeSymbol.List, TypeSymbol.Int) //TODO: Generic lists?
    };
}