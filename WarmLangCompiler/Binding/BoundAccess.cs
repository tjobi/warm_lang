using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public abstract class BoundAccess 
{
    public BoundAccess(TypeSymbol type)
    {
        Type = type;
    }

    public TypeSymbol Type { get; }
}

public sealed class BoundInvalidAccess : BoundAccess 
{
    public BoundInvalidAccess(): base(TypeSymbol.Error) { }
}

public sealed class BoundNameAccess : BoundAccess
{
    public BoundNameAccess(VariableSymbol symbol) : base(symbol.Type)
    {
        Symbol = symbol;
    }

    public VariableSymbol Symbol { get; }
}

public sealed class BoundExprAccess : BoundAccess
{
    public BoundExprAccess(BoundExpression expression) : base(expression.Type)
    {
        Expression = expression;
    }

    public BoundExpression Expression { get; }
}

public sealed class BoundSubscriptAccess : BoundAccess
{
    public BoundSubscriptAccess(BoundAccess target, BoundExpression index) : base(target.Type.NestedTypeOrThis())
    {
        Target = target;
        Index = index;
    }

    public BoundAccess Target { get; }
    public BoundExpression Index { get; }
}