using System.Text;
using WarmLangLexerParser;
using WarmLangLexerParser.ErrorReporting;

namespace WarmLangCompiler.ILGen;

internal static class EmitterErrorWarnings 
{
    internal static void ReportRequiredTypeProblems(this ErrorWarrningBag bag, string typeName, string ilName)
    {
        var message = $"Something went wrong when trying to find the IL-type '{ilName}' for our own type '{typeName}'";
        bag.Report(message, true, new TextLocation(0,0));
    }

    internal static void ReportRequiredMethodProblems(this ErrorWarrningBag bag, string classType, string methodName, string[] parameterTypeNames)
    {
        var sb = new StringBuilder($"Something went wrong trying to find method '{classType}.{methodName}(");
        foreach(var @param in parameterTypeNames)
        {
            sb.Append(@param);
            if(@param != parameterTypeNames[^1])
                sb.Append(", ");
        }
        sb.Append(")'");
        bag.Report(sb.ToString(), true, new TextLocation(0,0));
    }
}