using Mono.Cecil;
using WarmLangCompiler.Symbols;


namespace WarmLangCompiler.ILGen;
public sealed class WLTypeInformation
{
    public WLTypeInformation(TypeSymbol wltype, TypeDefinition typeDef, MethodDefinition constructor, Dictionary<MemberSymbol, FieldReference> memberSymbolToField)
    {
        WlType = wltype;
        TypeDef = typeDef;
        Constructor = constructor;
        SymbolToField = memberSymbolToField;
    }

    public TypeSymbol WlType { get; }
    public TypeDefinition TypeDef { get; }
    public MethodDefinition Constructor { get; }
    public Dictionary<MemberSymbol, FieldReference> SymbolToField { get; }
}