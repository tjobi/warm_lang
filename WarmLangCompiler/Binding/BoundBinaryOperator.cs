using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;
using static WarmLangCompiler.Symbols.TypeSymbol;
using static WarmLangCompiler.Binding.BoundBinaryOperatorKind;

namespace WarmLangCompiler.Binding;

public sealed class BoundBinaryOperator
{
    private BoundBinaryOperator(TokenKind op, BoundBinaryOperatorKind kind, TypeSymbol type)
    : this(op, kind, type, type, type)
    { }
    public BoundBinaryOperator(TokenKind opKind, BoundBinaryOperatorKind kind, TypeSymbol typeLeft, TypeSymbol typeRight, TypeSymbol resultType)
    {
        OpTokenKind = opKind;
        Kind = kind;
        TypeLeft = typeLeft;
        TypeRight = typeRight;
        Type = resultType;
    }

    public TokenKind OpTokenKind { get; }
    public BoundBinaryOperatorKind Kind { get; }
    public TypeSymbol TypeLeft { get; }
    public TypeSymbol TypeRight { get; }
    public TypeSymbol Type { get; } //ResultType

    public static BoundBinaryOperator? Bind(BinderTypeScope typeScope, TokenKind op, BoundExpression left, BoundExpression right)
    {
        for (int i = 0; i < _definedOperators.Length; i++)
        {
            var dop = _definedOperators[i];
            if(dop.OpTokenKind == op && typeScope.TypeEquality(left.Type, dop.TypeLeft) && typeScope.TypeEquality(right.Type, dop.TypeRight))
            {
                return dop;
            }
        }
        return OperatorOnGenericType(typeScope, op, left.Type, right.Type);
    }

    private static BoundBinaryOperator? OperatorOnGenericType(BinderTypeScope typeScope, TokenKind op, TypeSymbol left, TypeSymbol right)
    {
        var isLeftList = typeScope.IsListTypeAndGetNested(left, out var leftNested);
        var isRightList = typeScope.IsListTypeAndGetNested(right, out var rightNested);
        if(isLeftList && isRightList)
        {
            var sameNested = typeScope.TypeEquality(leftNested!,rightNested!);
            return op switch 
            {
                //List concat '+' operator
                TPlus when sameNested => new(op, ListConcat, left, right, left),
                //List equality '==' & '!=' operator
                TEqualEqual when sameNested => new(op, BoundBinaryOperatorKind.Equals, left, right, Bool),
                TBangEqual when sameNested => new(op, NotEquals, left, right, Bool),
                _ => null,
            };
        }

        //TODO: Should we cache these?
        return (op, left, right) switch
        {
            //List add '::' operator
            (TDoubleColon, _, _) when isLeftList && typeScope.TypeEquality(leftNested!, right) => new(op, ListAdd, left, right, left),
            //Operators on null
            (TEqualEqual, _, _)
                when left == Null && !right.IsValueType
                 || right == Null && !left.IsValueType => new(op, BoundBinaryOperatorKind.Equals, Bool),
            (TBangEqual, _, _)
                when left == Null && !right.IsValueType
                 || right == Null && !left.IsValueType => new(op, NotEquals, Bool),
            //No operator matches
            _ => null,
        };
    }

    private static readonly BoundBinaryOperator[] _definedOperators = new BoundBinaryOperator[]{
        //Basic int operators
        new(TPlus, Addition, Int),
        new(TStar, Multiplication, Int),
        new(TMinus, Subtraction, Int),
        new(TSlash, Division, Int),
        new(TDoubleStar, Power, Int),
        //Equaility and relation on ints
        new(TLessThan, LessThan, Int, Int, Bool),
        new(TLessThanEqual, LessThanEqual, Int, Int, Bool),
        new(TGreaterThan, GreaterThan, Int, Int, Bool),
        new(TGreaterThanEqual, GreaterThanEqual, Int, Int, Bool),
        new(TEqualEqual, BoundBinaryOperatorKind.Equals, Int, Int, Bool),
        new(TBangEqual, NotEquals, Int, Int, Bool),
        
        //builin for bools
        new(TEqualEqual, BoundBinaryOperatorKind.Equals, Bool),
        new(TBangEqual, NotEquals, Bool),
        new(TSeqAND, LogicAND, Bool),
        new(TSeqOR, LogicOR, Bool),

        //builtin for string 
        new(TPlus, StringConcat, TypeSymbol.String),
        new(TEqualEqual, BoundBinaryOperatorKind.Equals, TypeSymbol.String, TypeSymbol.String, Bool),
        new(TBangEqual, NotEquals, TypeSymbol.String, TypeSymbol.String, Bool),
    };
}
