namespace WarmLangCompiler.Symbols;

public static class BuiltinMembers
{
    internal static Dictionary<TypeSymbol, IList<MemberSymbol>> CreateMembersForBuiltins()
    {
        var members = new Dictionary<TypeSymbol, IList<MemberSymbol>>
        {
            [TypeSymbol.String]   = StringMembers().ToList(),
            [TypeSymbol.List] = ListMembers().ToList(),
        };
        return members;
    }

    private static IEnumerable<MemberSymbol> StringMembers()
    {
        yield return new MemberFieldSymbol("len", TypeSymbol.Int, isReadOnly:true, isBuiltin: true);
    }

    private static IEnumerable<MemberSymbol> ListMembers()
    {
        yield return new MemberFieldSymbol("len", TypeSymbol.Int, isReadOnly:true, isBuiltin: true);
    }
}