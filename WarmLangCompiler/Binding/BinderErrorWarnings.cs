using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Binding;

internal static class BinderErrorWarnings
{
    internal static void ReportBinaryOperatorCannotBeApplied(this ErrorWarrningBag bag, TextLocation loc, SyntaxToken op, TypeSymbol left, TypeSymbol right)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}' and '{right.Name}' ";
        bag.Report(message, true, loc);
    }

    internal static void ReportUnaryOperatorCannotBeApplied(this ErrorWarrningBag bag, TextLocation loc, SyntaxToken op, TypeSymbol left)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}'";
        bag.Report(message, true, loc);
    }

    internal static void ReportNameAlreadyDeclared(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"A variable or function named '{name}' is already defined in this scope";
        bag.Report(message, true, location);
    }

    internal static void ReportNameAlreadyDeclared(this ErrorWarrningBag bag, SyntaxToken token)
    => ReportNameAlreadyDeclared(bag,token.Location, token.Name!);

    internal static void ReportNameDoesNotExist(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"The name '{name}' does not exist in current scope";
        bag.Report(message, true, location);
    }

    internal static void ReportNameDoesNotExist(this ErrorWarrningBag bag, SyntaxToken name) => ReportNameDoesNotExist(bag, name.Location, name.Name!);

    internal static void ReportParameterDuplicateName(this ErrorWarrningBag bag, SyntaxToken name)
    {
        var message = $"The parameter '{name.Name}' is a duplicate";
        bag.Report(message, true, name.Location);
    }

    internal static void ReportCannotImplicitlyConvertToType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot implicitly convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotConvertToType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportCannotSubscriptIntoType(this ErrorWarrningBag bag, TextLocation location, TypeSymbol badBoi)
    {
        var message = $"Cannot apply subscripting [] to expression of type '{badBoi.Name}'";
        bag.Report(message, true, location);
    }

    internal static void ReportFunctionCallMismatchArguments(this ErrorWarrningBag bag, SyntaxToken called, int expectedNumArgs, int realNumArgs)
    {
        //TODO: Go through the arguments, and find the ones that are missing.
        var message = $"Function '{called.Name}' expected {expectedNumArgs} arguments but got {realNumArgs}";
        bag.Report(message, true, called.Location);
    }

    internal static void ReportNameIsNotAFunction(this ErrorWarrningBag bag, SyntaxToken called)
    {
        var message = $"The name '{called.Name}' is not a function and thus cannot be called";
        bag.Report(message, true, called.Location);
    }    

    internal static void ReportCannotReturnOutsideFunction(this ErrorWarrningBag bag, SyntaxToken retToken)
    {
        var message = $"Invalid 'return' outside of function body";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportReturnIsMissingExpression(this ErrorWarrningBag bag, SyntaxToken retToken, TypeSymbol expectedType)
    {
        var message = $"The return requires an expression of type '{expectedType}'";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportNotAllCodePathsReturn(this ErrorWarrningBag bag, FunctionSymbol function)
    {
        var message = $"All code paths of '{function}' must return value of type '{function.Type}'";
        bag.Report(message, true, function.Location);
    }

    internal static void ReportReturnWithValueInVoidFunction(this ErrorWarrningBag bag, SyntaxToken retToken, FunctionSymbol function)
    {
        var message = $"The function '{function}' returns void, so the return keyword must not be followed by an expression";
        bag.Report(message, true, retToken.Location);
    }

    internal static void ReportInvalidLeftSideOfAssignment(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = $"The left-hand side of an assignment must be a variable or a subscript";
        bag.Report(message, true, location);
    }

    internal static void ReportTypeOfEmptyListMustBeExplicit(this ErrorWarrningBag bag, TextLocation location)
    {
        var message = $"The type of empty list must be explicit unless used for variable declaration";
        bag.Report(message, true, location);
    }

    internal static void ReportSubscriptTargetIsReadOnly(this ErrorWarrningBag bag, TypeSymbol type, TextLocation location)
    {
        var message = $"Cannot assign to subscript of '{type}' -- it is readonly";
        bag.Report(message, true, location);
    }
    
}