using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundStructInitExpression : BoundExpression
{
    public BoundStructInitExpression(ExpressionNode node, TypeSymbol type, ImmutableArray<(MemberSymbol, BoundExpression)> initializedMembers) : base(node, type)
    {
        InitializedMembers = initializedMembers;
    }

    public ImmutableArray<(MemberSymbol, BoundExpression)> InitializedMembers { get; }
}