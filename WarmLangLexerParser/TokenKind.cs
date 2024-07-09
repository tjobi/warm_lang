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


    //Keyword
    TFunc, //function keyword 
    TIf, TElse, //To allow if <cond> <block-statement> else <block-statement>
    TWhile,
    TReturn,

    //"Kind of construct?"
    TConst, TVar, TInt, //var x = 5, the keyword 'var'
    TIdentifier, //Variable names, function names...
    TArray,
}
