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
        return OperatorOnGenericType(op, left.Type);
    }

    private static BoundUnaryOperator? OperatorOnGenericType(TokenKind op, TypeSymbol left)
    {
        //TODO: Should we cache these?
        return (op,left) switch
        {
            (TLeftArrow, ListTypeSymbol lts) => new BoundUnaryOperator(op, left, lts.InnerType),
            _ => null, 
        };
    }

    private static readonly BoundUnaryOperator[] _definedOperators = new BoundUnaryOperator[]
    {
        new(TPlus, TypeSymbol.Int, TypeSymbol.Int),
        new(TMinus, TypeSymbol.Int, TypeSymbol.Int),
        new(TLeftArrow, TypeSymbol.IntList, TypeSymbol.Int),
        new(TBang, TypeSymbol.Bool, TypeSymbol.Bool),
    };
}