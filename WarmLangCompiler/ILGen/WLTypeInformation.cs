using Mono.Cecil;
using WarmLangCompiler.Symbols;


namespace WarmLangCompiler.ILGen;
public sealed class WLTypeInformation
{
    public WLTypeInformation(TypeSymbol wltype, TypeDefinition typeDef, MethodDefinition constructor, Dictionary<string, FieldReference> memberSymbolToField)
    {
        WlType = wltype;
        TypeDef = typeDef;
        Constructor = constructor;
        SymbolToField = memberSymbolToField;
    }

    public WLTypeInformation(TypeSymbol wltype, MethodReference constructor, Dictionary<string, FieldReference> memberSymbolToField)
    {
        WlType = wltype;
        TypeDef = null!;
        Constructor = constructor;
        SymbolToField = memberSymbolToField;
    }

    public TypeSymbol WlType { get; }
    public TypeDefinition TypeDef { get; }
    public MethodReference Constructor { get; }
    public Dictionary<string, FieldReference> SymbolToField { get; }
}