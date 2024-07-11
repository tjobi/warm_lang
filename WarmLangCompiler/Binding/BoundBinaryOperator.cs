using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using static WarmLangLexerParser.TokenKind;
using static WarmLangCompiler.Symbols.TypeSymbol;

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
        new(TPlus, Int),
        new(TStar, Int),
        new(TMinus, Int),
        new(TSlash, Int),
        new(TDoubleStar, Int),
        //Equaility and relation on ints
        new(TLessThan, Int, Int, Bool),
        new(TLessThanEqual, Int, Int, Bool),
        new(TGreaterThan, Int, Int, Bool),
        new(TGreaterThanEqual, Int, Int, Bool),
        new(TEqualEqual, Int, Int, Bool),
        new(TBangEqual, Int, Int, Bool),
        
        //builin for bools
        new(TEqualEqual, Bool),
        new(TBangEqual , Bool),

        //builtin for string 
        new(TPlus, TypeSymbol.String),
        new(TEqualEqual, TypeSymbol.String, TypeSymbol.String, Bool),

        //builtin list operators
        new(TDoubleColon, IntList, Int, IntList),
        new(TPlus,IntList),
    };
}