namespace WarmLangCompiler.Symbols;

public abstract class VariableSymbol : EntitySymbol
{
    public VariableSymbol(string name, TypeSymbol type) 
    : base(name, type) { }
    public override string ToString() => $"{Type} {Name}";

    public bool IsLocalVariableAndFree => this is LocalVariableSymbol local && local.IsFree; 
}

public sealed class LocalVariableSymbol : VariableSymbol
{
    public LocalVariableSymbol(string name, TypeSymbol type, FunctionSymbol belongsTo) : base(name, type)
    {
        BelongsTo = belongsTo;
        IsFree = false;
    }

    public bool IsFree { get; set; }
    public FunctionSymbol BelongsTo { get; }
}