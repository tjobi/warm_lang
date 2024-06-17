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
        return OperatorOnGenericType(op, left.Type, right.Type);
    }

    private static BoundBinaryOperator? OperatorOnGenericType(TokenKind op, TypeSymbol left, TypeSymbol right)
    {
        //TODO: Should we cache these?
        return (op,left,right) switch
        {
            (TPlus, _, ListTypeSymbol _) when left == TypeSymbol.EmptyList => new BoundBinaryOperator(op, left, right, right),
            (TPlus, ListTypeSymbol lts1, ListTypeSymbol lts2) when lts1.InnerType == lts2.InnerType
                => new BoundBinaryOperator(op, left, right, lts2.InnerType),
            _ => null, 
        };
    }

    private static BoundBinaryOperator[] _definedOperators = new BoundBinaryOperator[]{
        //Basic int operators
        new(TPlus, TypeSymbol.Int),
        new(TStar, TypeSymbol.Int),
        new(TMinus, TypeSymbol.Int),
        new(TLessThan, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Int),
        new(TLessThanEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Int),
        
        //builtin list operators
        new(TDoubleColon, TypeSymbol.IntList, TypeSymbol.Int, TypeSymbol.IntList),
        new(TPlus,TypeSymbol.IntList),
    };
}