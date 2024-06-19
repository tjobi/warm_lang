using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Binding;

internal static class BinderErrorWarnings
{
    internal static void ReportBinaryOperatorCannotBeApplied(this ErrorWarrningBag bag, SyntaxToken op, TypeSymbol left, TypeSymbol right)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}' and '{right.Name}' ";
        bag.Report(message, true, op.Line, op.Column);
    }

    internal static void ReportUnaryOperatorCannotBeApplied(this ErrorWarrningBag bag, SyntaxToken op, TypeSymbol left)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left.Name}'";
        bag.Report(message, true, op.Line, op.Column);
    }

    internal static void ReportVariableAlreadyDeclared(this ErrorWarrningBag bag, string name)
    {
        var message = $"Variable '{name}' is already defined in this scope";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportFunctionAlreadyDeclared(this ErrorWarrningBag bag, string name)
    {
        var message = $"Function '{name}' is already defined in this scope";
        bag.Report(message, true, 0,0);
    }
    internal static void ReportNameDoesNotExist(this ErrorWarrningBag bag, string name)
    {
        var message = $"The name '{name}' does not exist in current scope";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportParameterDuplicateName(this ErrorWarrningBag bag, string name)
    {
        var message = $"The parameter '{name}' is a duplicate";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportCannotImplicitlyConvertToType(this ErrorWarrningBag bag, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot implicitly convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportCannotConvertToType(this ErrorWarrningBag bag, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportCannotSubscriptIntoType(this ErrorWarrningBag bag, TypeSymbol badBoi)
    {
        var message = $"Cannot apply subscripting [] to expression of type '{badBoi.Name}'";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportFunctionCalMissingArguments(this ErrorWarrningBag bag, string name, int expectedNumArgs, int realNumArgs)
    {
        //TODO: Go through the arguments, and find the ones that are missing.
        var message = $"Function '{name}' expected {expectedNumArgs} arguments but got {realNumArgs}";
        bag.Report(message, true, 0,0);
    }

    internal static void ReportNameIsNotAFunction(this ErrorWarrningBag bag, string name)
    {
        var message = $"The name '{name}' is not a function and thus cannot be called";
        bag.Report(message, true, 0,0);
    }    

    
}