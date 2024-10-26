namespace WarmLangLexerParser;

public enum TokenKind
{
    //Token
    TBadToken, TEOF,
    TSemiColon, TComma, TDot, TColon, TDoubleColon, TColonBang, TLeftArrow,
    TEqual, TEqualEqual,
    TLessThan, TLessThanEqual, TGreaterThan, TGreaterThanEqual,
    TBang, TBangEqual,
    TPlus, TStar, TSlash, TMinus, TDoubleStar,
    TParentheses, TCurLeft, TCurRight, TParLeft, TParRight, TBracketLeft, TBracketRight, /*Brackets [] */
    TQuote, TTick, // " and '
    TSeqAND, TSeqOR, // '&&' and '||' -> Logical AND OR. 
    TAmp, TBar, // '&' and '|'


    //Keyword
    TFunc, //function keyword 
    TIf, TElse, //To allow if <cond> <block-statement> else <block-statement>
    TWhile,
    TReturn,
    TTrue, TFalse,
    TVar,//var x = 5, the keyword 'var'
    TType, TNew,

    //"Kind of construct?"
    TConst, TInt, TBool, TString, TStringLiteral,
    TIdentifier, //Variable names, function names...
    TArray,
}
