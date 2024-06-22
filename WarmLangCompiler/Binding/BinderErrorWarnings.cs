using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Binding;

internal static class BinderErrorWarnings
{
    internal static void ReportBinaryOperatorCannotBeApplied(this ErrorWarrningBag bag, SyntaxToken op, TypeSymbol left, TypeSymbol right)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}' and '{right.Name}' ";
        bag.Report(message, true, op.Location);
    }

    internal static void ReportUnaryOperatorCannotBeApplied(this ErrorWarrningBag bag, SyntaxToken op, TypeSymbol left)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}'";
        bag.Report(message, true, op.Location);
    }

    internal static void ReportVariableAlreadyDeclared(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"Variable '{name}' is already defined in this scope";
        bag.Report(message, true, location);
    }

    internal static void ReportFunctionAlreadyDeclared(this ErrorWarrningBag bag, TextLocation location, string name)
    {
        var message = $"Function '{name}' is already defined in this scope";
        bag.Report(message, true, location);
    }
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

    internal static void ReportCannotSubscriptIntoType(this ErrorWarrningBag bag, TypeSymbol badBoi)
    {
        var message = $"Cannot apply subscripting [] to expression of type '{badBoi.Name}'";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportFunctionCalMissingArguments(this ErrorWarrningBag bag, SyntaxToken called, int expectedNumArgs, int realNumArgs)
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

    
}