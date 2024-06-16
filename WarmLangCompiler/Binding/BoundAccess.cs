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
    public BoundNameAccess(VariableSymbol name) : base(name.Type)
    {
        Name = name;
    }

    public VariableSymbol Name { get; }
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
    private static TypeSymbol GetType(BoundAccess target)
    {
        if(target.Type is ListTypeSymbol lts)
        {
            return lts.InnerType;
        }
        return target.Type;
    }
    public BoundSubscriptAccess(BoundAccess target, BoundExpression index) : base(GetType(target))
    {
        Target = target;
        Index = index;
    }

    public BoundAccess Target { get; }
    public BoundExpression Index { get; }
}