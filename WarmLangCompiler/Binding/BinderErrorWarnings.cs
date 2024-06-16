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

    internal static void ReportCannotImplicitlyConvertToType(this ErrorWarrningBag bag, TypeSymbol expected, TypeSymbol badBoi)
    {
        var message = $"Cannot implicitly convert type '{badBoi.Name}' to '{expected.Name}'";
        bag.Report(message, true, 0,0);
    }

    
}