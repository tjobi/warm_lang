namespace WarmLangCompiler.Symbols;

public abstract class VariableSymbol(string name, TypeSymbol type) : EntitySymbol(name, type)
{
    public override string ToString() => $"{Type} {Name}";
}

public abstract class ScopedVariableSymbol : VariableSymbol
{
    public ScopedVariableSymbol(string name, TypeSymbol type)
    : base(name, type) { }

    public ScopedVariableSymbol(string name, TypeSymbol type, FunctionSymbol belongsTo)
    : this(name, type)
    {
        BelongsTo = belongsTo;
    }

    public FunctionSymbol? BelongsTo { get; set; }

    public FunctionSymbol BelongsToOrThrow => BelongsTo ?? throw new Exception($"Some assumption is wrong - {this} doesn't belong to any function");
}

public sealed class LocalVariableSymbol : ScopedVariableSymbol
{
    public LocalVariableSymbol(string name, TypeSymbol type, FunctionSymbol belongsTo)
    : base(name, type, belongsTo) { }
}