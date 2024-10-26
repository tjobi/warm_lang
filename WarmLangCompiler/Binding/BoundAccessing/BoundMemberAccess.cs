using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundMemberAccess : BoundTargetedAccess
{
    public BoundMemberAccess(BoundAccess target, MemberSymbol member) : base(target, member.Type)
    {
        Member = member;
    }
    public MemberSymbol Member { get; }
}
