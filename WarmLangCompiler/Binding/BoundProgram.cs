using System.Collections.Immutable;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundProgram
{
    public BoundProgram(FunctionSymbol? mainFunc, FunctionSymbol? scriptMain, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ImmutableArray<BoundVarDeclaration> globalVariables)
    {
        MainFunc = mainFunc;
        ScriptMain = scriptMain;
        Functions = functions;
        GlobalVariables = globalVariables;
    }

    public FunctionSymbol? MainFunc { get; }
    public FunctionSymbol? ScriptMain { get; }
    public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
    public ImmutableArray<BoundVarDeclaration> GlobalVariables { get; }

    public bool IsValid => MainFunc is not null || ScriptMain is not null;

    public override string ToString()
    {
        if(!IsValid)
            return "Non-valid program - not entry points";
        var entry = MainFunc is null ? ScriptMain! : MainFunc;
        return entry.ToString() + " " + Functions[entry];
    }
}
