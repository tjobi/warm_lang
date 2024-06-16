using WarmLangCompiler.Symbols;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.Binding;

internal static class BinderErrorWarnings
{
    internal static void ReportBinaryOperatorCannotBeApplied(this ErrorWarrningBag bag, SyntaxToken op, TypeSymbol left, TypeSymbol right)
    {
        var message = $"Operator '{op.Kind.AsString()}' cannot be applied to type '{left}' and '{right}' ";
        bag.Report(message, true, op.Line, op.Column);
    }
}