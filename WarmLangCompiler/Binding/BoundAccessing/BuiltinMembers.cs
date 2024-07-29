using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public static class BuiltinMembers
{
    internal static Dictionary<TypeSymbol, IList<MemberSymbol>> CreateMembersForBuiltins()
    {
        var members = new Dictionary<TypeSymbol, IList<MemberSymbol>>
        {
            [TypeSymbol.String]   = StringMembers().ToList(),
            [TypeSymbol.ListBase] = StringMembers().ToList(),
        };
        return members;
    }

    private static IEnumerable<MemberSymbol> StringMembers()
    {
        yield return new MemberFieldSymbol("len", TypeSymbol.Int);
    }

    private static IEnumerable<MemberSymbol> ListMembers()
    {
        yield return new MemberFieldSymbol("len", TypeSymbol.Int);
    }
}