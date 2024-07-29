using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding.BoundAccessing;

public sealed class BoundMemberAccess : BoundAccess
{
    public BoundMemberAccess(BoundAccess target, MemberSymbol member) : base(member.Type)
    {
        Target = target;
        Member = member;
    }

    public BoundAccess Target { get; }
    public MemberSymbol Member { get; }
    public override bool HasNested => true;
}
