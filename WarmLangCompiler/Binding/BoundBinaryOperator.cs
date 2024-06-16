using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;

namespace WarmLangCompiler.Binding;

public sealed class BoundBinaryOperator
{
    private BoundBinaryOperator(TokenKind op, TypeSymbol type)
    : this(op, type, type, type)
    { }
    public BoundBinaryOperator(TokenKind op, TypeSymbol typeLeft, TypeSymbol typeRight, TypeSymbol resultType)
    {
        Kind = op;
        TypeLeft = typeLeft;
        TypeRight = typeRight;
        Type = resultType;
    }

    public TokenKind Kind { get; }
    public TypeSymbol TypeLeft { get; }
    public TypeSymbol TypeRight { get; }
    public TypeSymbol Type { get; } //ResultType

    public static BoundBinaryOperator? Bind(TokenKind op, BoundExpression left, BoundExpression right)
    {
        for (int i = 0; i < _definedOperators.Length; i++)
        {
            var dop = _definedOperators[i];
            if(dop.Kind == op && left.Type == dop.TypeLeft && right.Type == dop.TypeRight)
            {
                return dop;
            }
        }
        return null;
    }
    private static BoundBinaryOperator[] _definedOperators = new BoundBinaryOperator[]{
        //Basic int operators
        new(TPlus, TypeSymbol.Int),
        new(TStar, TypeSymbol.Int),
        new(TMinus, TypeSymbol.Int),
        new(TLessThan, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Int),
        new(TLessThanEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Int),
        
        //builtin list operators
        new(TDoubleColon, TypeSymbol.List, TypeSymbol.Int, TypeSymbol.List),
        new(TPlus,TypeSymbol.List),
    };
}