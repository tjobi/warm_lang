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
            //List concat '+' operator
            (TPlus, ListTypeSymbol lts1, ListTypeSymbol lts2) when lts1.InnerType == lts2.InnerType
                => new BoundBinaryOperator(op, left, right, lts2.InnerType),

            //List add '::' operator
            (TDoubleColon, ListTypeSymbol lts1, _) when lts1.InnerType == right => new BoundBinaryOperator(op, left, right, left),
            
            //List equality '==' & '!=' operator
            (TEqualEqual or TBangEqual, ListTypeSymbol lts1, ListTypeSymbol lts2) 
                when lts1.InnerType == lts2.InnerType => new(op, left, right, TypeSymbol.Bool),
            
            //No operator matches
            _ => null, 
        };
    }

    private static readonly BoundBinaryOperator[] _definedOperators = new BoundBinaryOperator[]{
        //Basic int operators
        new(TPlus, TypeSymbol.Int),
        new(TStar, TypeSymbol.Int),
        new(TMinus, TypeSymbol.Int),
        new(TSlash, TypeSymbol.Int),
        new(TDoubleStar, TypeSymbol.Int),
        //Equaility and relation on ints
        new(TLessThan, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        new(TLessThanEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        new(TGreaterThan, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        new(TGreaterThanEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        new(TEqualEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        new(TBangEqual, TypeSymbol.Int, TypeSymbol.Int, TypeSymbol.Bool),
        
        //builtin list operators
        new(TDoubleColon, TypeSymbol.IntList, TypeSymbol.Int, TypeSymbol.IntList),
        new(TDoubleColon, TypeSymbol.EmptyList, TypeSymbol.Int, TypeSymbol.IntList),
        new(TPlus,TypeSymbol.IntList),
    };
}