using System.Collections.ObjectModel;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

public sealed class BinderTypeHelper
{
    private readonly Dictionary<TypeSymbol, IList<MemberSymbol>> _typeMembers;

    public BinderTypeHelper()
    {
        _typeMembers = BuiltinMembers.CreateMembersForBuiltins();
    }

    private bool NotSeen(TypeSymbol type) => !_typeMembers.ContainsKey(type);

    public bool Has(TypeSymbol type) => !NotSeen(type);

    public ReadOnlyDictionary<TypeSymbol, IList<MemberSymbol>> TypeMembers => new(_typeMembers);

    public bool TryAddType(TypeSymbol type)
    {
        if(Has(type))
            return false;
        _typeMembers[type] = new List<MemberSymbol>();
        return true;
    }

    public void AddMember(TypeSymbol type, MemberSymbol member)
    {
        if(NotSeen(type))
            _typeMembers[type] = new List<MemberSymbol>();
        else
            throw new Exception($"{nameof(BinderTypeHelper)} - the member '{type}.{member}' is already defined!");
        _typeMembers[type].Add(member);
    }

    public MemberSymbol? FindMember(TypeSymbol type, string name)
    {
        if(type is ListTypeSymbol)
            type = TypeSymbol.ListBase;
        if(NotSeen(type))
            throw new Exception($"{nameof(BinderTypeHelper)} - hasn't seen '{type}'");
        if(name is null) //TODO: Do we need to?
            throw new ArgumentNullException(nameof(name));

        foreach(var member in _typeMembers[type])
        {
            if(member.Name == name)
                return member;
        }
        return null;
    }
}