using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;

namespace WarmLangCompiler.Binding;

using TypeMemberDict = Dictionary<TypeSymbol, IList<MemberSymbol>>;
using TypeMemberFuncDict = Dictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>;

public sealed class BinderTypeHelper
{
    private readonly TypeMemberDict _typeMembers;
    private readonly TypeMemberFuncDict _typeFunctions;

    private readonly ISet<TypeSymbol> _declaredTypes;

    public BinderTypeHelper()
    {
        _typeMembers = BuiltinMembers.CreateMembersForBuiltins();
        _typeFunctions = new();
        _declaredTypes = new HashSet<TypeSymbol>();
    }

    private bool NotSeen(TypeSymbol type) => !_typeMembers.ContainsKey(type);

    public bool Has(TypeSymbol type) => !NotSeen(type);

    public TypeMemberInformation ToTypeMemberInformation()
    {
        var members = new ReadOnlyDictionary<TypeSymbol, IList<MemberSymbol>>(_typeMembers);
        var funcs = new ReadOnlyDictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>(_typeFunctions);
        return new TypeMemberInformation(members, funcs, _declaredTypes.ToList().AsReadOnly());
    }

    public bool TryAddType(TypeSymbol type)
    {
        if(Has(type))
            return false;
        _typeMembers[type] = new List<MemberSymbol>();
        _declaredTypes.Add(type);
        return true;
    }

    public bool TryGetTypeSymbol(string name, [NotNullWhen(true)] out TypeSymbol? res)
    {
        res = null;
        var tmp = new TypeSymbol(name);
        if(_typeMembers.ContainsKey(tmp)) res = tmp;
        return res is not null;
    }

    public void AddMember(TypeSymbol type, MemberSymbol member)
    {
        if(NotSeen(type))
            _typeMembers[type] = new List<MemberSymbol>();
        
        if(_typeMembers[type].Contains(member))
            throw new Exception($"{nameof(BinderTypeHelper)} - the member '{type}.{member}' is already defined!");
        _typeMembers[type].Add(member);
    }

    public MemberSymbol? FindMember(TypeSymbol type, string name)
    {
        if(type is ListTypeSymbol && _typeMembers.TryGetValue(TypeSymbol.ListBase, out var listBuiltins))
        {
            foreach(var bultinMember in listBuiltins) 
                if(bultinMember.Name == name) return bultinMember;
        }
        if(NotSeen(type))
            return null;
        if(name is null) //TODO: Do we need to?
            throw new ArgumentNullException(nameof(name));

        foreach(var member in _typeMembers[type])
        {
            if(member.Name == name)
                return member;
        }
        return null;
    }

    public bool TryFindMember(TypeSymbol type, string name, [NotNullWhen(true)] out MemberSymbol? res)
        => (res = FindMember(type, name)) is not null;
    

    public IEnumerable<FunctionSymbol> GetFunctionMembers(TypeSymbol type)
    {
        foreach(var member in _typeMembers[type])
        {
            if(member is MemberFuncSymbol mf)
                yield return mf.Function;
        }
    }

    public IEnumerable<(TypeSymbol Type, IEnumerable<FunctionSymbol> MemberFuncs)> GetFunctionMembers()
    {
        foreach(var typeMembers in _typeMembers)
        {
            var type = typeMembers.Key;
            yield return (type, GetFunctionMembers(type));
        }
    }

    public void BindMemberFunc(TypeSymbol type, FunctionSymbol function, BoundBlockStatement boundBody)
    {
        if(!_typeFunctions.ContainsKey(type))
            _typeFunctions[type] = new();
        _typeFunctions[type][function] = boundBody;
    }

    public bool ContainsTypeWithNameOf(string name) => Has(new TypeSymbol(name));
}