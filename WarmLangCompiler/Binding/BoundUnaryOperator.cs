using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangCompiler.Binding;

public sealed class BoundUnaryOperator
{
    public BoundUnaryOperator(BoundUnaryOperatorKind kind, TokenKind op, TypeSymbol left, TypeSymbol resultType)
    {
        Kind = kind;
        Operator = op;
        Type = resultType;
        TypeLeft = left;
    }

    public BoundUnaryOperatorKind Kind { get; }
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
            (TLeftArrow, ListTypeSymbol lts) => new BoundUnaryOperator(BoundUnaryOperatorKind.ListRemoveLast,op, left, lts.InnerType),
            _ => null, 
        };
    }

    private static readonly BoundUnaryOperator[] _definedOperators = new BoundUnaryOperator[]
    {
        new(BoundUnaryOperatorKind.UnaryPlus,  TPlus, TypeSymbol.Int, TypeSymbol.Int),
        new(BoundUnaryOperatorKind.UnaryMinus, TMinus, TypeSymbol.Int, TypeSymbol.Int),
        new(BoundUnaryOperatorKind.LogicalNOT, TBang, TypeSymbol.Bool, TypeSymbol.Bool),
    };
}

public enum BoundUnaryOperatorKind 
{
    UnaryMinus, UnaryPlus, LogicalNOT, ListRemoveLast
}