using System.Collections.Immutable;
using System.Collections.ObjectModel;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public record TypeMemberInformation(
    ReadOnlyDictionary<TypeSymbol, TypeInformation> TypeInformation,
    ReadOnlyCollection<TypeSymbol> DeclaredTypes
);

public sealed class BoundProgram
{
    public BoundProgram(FunctionSymbol? mainFunc, FunctionSymbol? scriptMain, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, TypeMemberInformation typeMembers,ImmutableArray<BoundVarDeclaration> globalVariables)
    {
        MainFunc = mainFunc;
        ScriptMain = scriptMain;
        Functions = functions;
        TypeInformation = typeMembers.TypeInformation;
        DeclaredTypes = typeMembers.DeclaredTypes;
        GlobalVariables = globalVariables;
    }

    public FunctionSymbol? MainFunc { get; }
    public FunctionSymbol? ScriptMain { get; }
    public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
    public ReadOnlyDictionary<TypeSymbol, TypeInformation> TypeInformation { get; }
    public ReadOnlyCollection<TypeSymbol> DeclaredTypes { get; }
    public ImmutableArray<BoundVarDeclaration> GlobalVariables { get; }

    public bool IsValid => MainFunc is not null || ScriptMain is not null;

    public BoundBlockStatement Entry => IsValid ? Functions[MainFunc ?? ScriptMain!] : throw new Exception("INVALID BOUND PROGRAM");

    public IEnumerable<(TypeSymbol Type, IList<MemberSymbol> Members)> GetDeclaredTypes()
    {
        foreach(var t in DeclaredTypes)
            yield return (t, TypeInformation[t].Members);
    }

    public IEnumerable<FunctionSymbol> GetFunctionSymbols()
    {
        foreach(var (func, _) in GetFunctionSymbolsAndBodies())
            yield return func;
    }

    public IEnumerable<(FunctionSymbol Func, BoundBlockStatement Body)> GetFunctionSymbolsAndBodies()
    {
        foreach(var (type, typeInfo) in TypeInformation)
            foreach(var (func, body) in typeInfo.MethodBodies)
                yield return (func, body);

        foreach(var (func, body) in Functions)
            yield return (func, body);
    }

    public override string ToString()
    {
        if(!IsValid)
            return "Non-valid program - no entry points";
        var entry = MainFunc is null ? ScriptMain! : MainFunc;
        return entry.ToString() + " " + Functions[entry];
    }
}
