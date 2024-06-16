namespace WarmLangLexerParser;

public enum TokenKind
{
    //Token
    TBadToken, TEOF,
    TIdentifier, //Variable names, function names...
    TVar, TInt, //var x = 5, the keyword 'var'
    TSemiColon, TComma, TDot, TColon, TDoubleColon, TBang, TColonBang, TLeftArrow,
    TEqual,
    TEqualEqual, TLessThan, TLessThanEqual, TPlus, TStar, TSlash, TMinus,
    TParentheses, TCurLeft, TCurRight, TParLeft, TParRight, TBracketLeft, TBracketRight, /*Brackets [] */


    //Keyword
    TFunc, //function keyword 
    TIf, TThen, TElse, //To allow if <cond> then <statement> else <statement>


    //"Kind of construct?"
    TConst, TArray,
    TAssign, //Assignment x = 10;
    TBlock, TWhile,
    TCall, //function call
    TIfStmnt,
}
