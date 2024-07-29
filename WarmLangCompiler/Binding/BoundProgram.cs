using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BoundProgram
{
    public BoundProgram(FunctionSymbol? mainFunc, FunctionSymbol? scriptMain, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, ReadOnlyDictionary<TypeSymbol, IList<MemberSymbol>> typeMembers,ImmutableArray<BoundVarDeclaration> globalVariables)
    {
        MainFunc = mainFunc;
        ScriptMain = scriptMain;
        Functions = functions;
        TypeMembers = typeMembers;
        GlobalVariables = globalVariables;
    }

    public FunctionSymbol? MainFunc { get; }
    public FunctionSymbol? ScriptMain { get; }
    public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
    public ReadOnlyDictionary<TypeSymbol, IList<MemberSymbol>> TypeMembers { get; }
    public ImmutableArray<BoundVarDeclaration> GlobalVariables { get; }

    public bool IsValid => MainFunc is not null || ScriptMain is not null;

    public BoundBlockStatement Entry => IsValid ? Functions[MainFunc ?? ScriptMain!] : throw new Exception("INVALID BOUND PROGRAM");

    public override string ToString()
    {
        if(!IsValid)
            return "Non-valid program - not entry points";
        var entry = MainFunc is null ? ScriptMain! : MainFunc;
        return entry.ToString() + " " + Functions[entry];
    }
}
