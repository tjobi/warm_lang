using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding;

public sealed class BoundObjectInitExpression : BoundExpression
{
    public BoundObjectInitExpression(ExpressionNode node, TypeSymbol type, ImmutableArray<(MemberSymbol, BoundExpression)> initializedMembers) : base(node, type)
    {
        InitializedMembers = initializedMembers;
    }

    public ImmutableArray<(MemberSymbol MemberSymbol, BoundExpression Rhs)> InitializedMembers { get; }
}