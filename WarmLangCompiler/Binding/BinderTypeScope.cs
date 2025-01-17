using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Binding;

using TypeFieldDict = Dictionary<TypeSymbol, IList<MemberSymbol>>;
using TypeMethodDict = Dictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>;

using InternalTypeScope = Dictionary<string, TypeInformation>;
public record TypeInformation(
    TypeSymbol Type,
    List<MemberSymbol> Members,
    Dictionary<FunctionSymbol, BoundBlockStatement> MethodBodies,
    TypeSymbol? SpecializedAs = null
)
{
    public TypeInformation(TypeSymbol type) 
    : this(type, new(), new()) { }

    public TypeInformation(TypeSymbol type, TypeSymbol SpecializedAs) 
    : this(type, new(), new(), SpecializedAs) { }

    public IEnumerable<FunctionSymbol> GetMethodFunctionSymbols()
    {
        foreach(var m in Members)
            if(m is MemberFuncSymbol mf) yield return mf.Function;
    }

    public IEnumerable<MemberFieldSymbol> GetFields()
    {
        foreach(var m in Members)
            if(m is MemberFieldSymbol mf) yield return mf;
    }
}


public sealed class BinderTypeScope
{
    private class BinderTypeScopeExeception : Exception 
    {
        public BinderTypeScopeExeception(string msg) : base(msg) {}
    }

    private static IEnumerable<TypeSymbol> BuiltinTypes() 
    {
        yield return TypeSymbol.Int;
        yield return TypeSymbol.Bool;
        yield return TypeSymbol.String;
        yield return TypeSymbol.ListBase;
        yield return TypeSymbol.Void;
    }

    private readonly List<InternalTypeScope> _scopes;
    private readonly HashSet<TypeSymbol> _userDefinedTypes;
    private InternalTypeScope Top => _scopes[^1];
    private InternalTypeScope Global => _scopes[0];
    

    public BinderTypeScope()
    {
        _scopes = new();
        _userDefinedTypes = new();
        var globalTypeScope = new InternalTypeScope();
        foreach(var builtin in BuiltinTypes()) 
            globalTypeScope.Add(builtin.Name, new(builtin));

        foreach(var (type, fields) in BuiltinMembers.CreateMembersForBuiltins())
        {
            globalTypeScope[type.Name] = new TypeInformation(type, fields.ToList(), new());
        }
        _scopes.Add(globalTypeScope);
    }

    public void Push() => _scopes.Add(new());
    public InternalTypeScope Pop()
    {
        var popped = _scopes[^1];
        _scopes.RemoveAt(_scopes.Count-1);
        return popped;
    }

    private (TypeSymbol Type, TypeInformation Info)? GetTypeAndInformation(TypeSymbol? type)
    {
        if(type is not null)
        {
            foreach(var scope in _scopes)
            {
                if(scope.TryGetValue(type.Name, out var info)) 
                    return (info.Type, info);
            }
        }
        if(type is ListTypeSymbol)
        {
            //List wasn't found, so we will create it - create a concrete type of list... list<int>, list<bool>...
            var info = new TypeInformation(type);
            Global.Add(type.Name, info);
            return (type, info);
        }
        return null;
    }

    public bool TryAddType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res, TypeInformation? typeInfo = null)
    {
        res = null;
        var knownTypeOrNull = GetType(type); 
        if(knownTypeOrNull is not null) return false;

        Top.Add(type.Name, typeInfo ?? new TypeInformation(type));
        res = type;
        if(type is not TypeParameterSymbol) _userDefinedTypes.Add(res);
        return true;
    }

    public bool TryAddType(TypeSyntaxNode typeSyntax, [NotNullWhen(true)] out TypeSymbol? res, TypeInformation? typeInfo = null)
        => TryAddType(AsTypeSymbol(typeSyntax), out res, typeInfo);

    public bool ContainsTypeWithNameOf(string name) 
        => GetType(new TypeSymbol(name)) is not null;

    public bool TryGetType(TypeSyntaxNode type, [NotNullWhen(true)] out TypeSymbol? res) 
        => TryGetType(AsTypeSymbol(type).Name, out res);
    
    public bool TryGetType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res)
        => TryGetType(type.Name, out res);

    public bool TryGetType(string typeName, [NotNullWhen(true)] out TypeSymbol? res)
        => (res = GetType(TypeSymbolFromString(typeName))) is not null;

    public TypeSymbol? GetType(TypeSyntaxNode? type) => GetType(AsTypeSymbol(type));

    public TypeSymbol? GetType(TypeSymbol? type)
    {
        var res = GetTypeAndInformation(type);
        if(res is null) return null;
        return res.Value.Type;
    }

    public TypeSymbol GetTypeOrCrash(TypeSyntaxNode? type) => GetTypeOrCrash(AsTypeSymbol(type));
    public TypeSymbol GetTypeOrCrash(TypeSymbol? type) 
        => GetType(type) ?? throw new BinderTypeScopeExeception($"{nameof(BinderTypeScope)} - cannot find '{type}'");

    private TypeSymbol AsTypeSymbol(TypeSyntaxNode? typeSyntax) 
    {
        if (typeSyntax is null) return TypeSymbol.Void; //Really?
        return typeSyntax switch
        {
            TypeSyntaxInt => TypeSymbol.Int,
            TypeSyntaxBool => TypeSymbol.Bool,
            TypeSyntaxString => TypeSymbol.String,
            TypeSyntaxIdentifier ident => TypeSymbolFromString(ident.Name),
            TypeSyntaxList lst => new ListTypeSymbol(AsTypeSymbol(lst.InnerType)),
            TypeSyntaxParameterType tv => new TypeParameterSymbol(tv),
            _ => throw new NotImplementedException($"BinderTypeExntensions doesn't know {typeSyntax}")
        };
    }

    //Something else that can be done?
    private static TypeSymbol TypeSymbolFromString(string s) => new(s);

    public List<(TypeSymbol Type, IEnumerable<FunctionSymbol> MemberFuncs)> GetFunctionMembers()
    {
        var res = new List<(TypeSymbol,IEnumerable<FunctionSymbol>)>();
        foreach(var scope in _scopes) 
        {
            foreach(var (_, info) in scope) 
            {
                res.Add((info.Type, info.GetMethodFunctionSymbols()));
            }
        }
        return res;
    }

    public TypeInformation? GetTypeInformation(TypeSymbol type)
    {
        var res = GetTypeAndInformation(type);
        if(res is null) return null;
        return res.Value.Info;
    }
    public bool TryGetTypeInformation(TypeSymbol type, [NotNullWhen(true)] out TypeInformation? typeInfo)
        => (typeInfo = GetTypeInformation(type)) is not null;

    public void AddMethodBody(TypeSymbol type, FunctionSymbol func, BoundBlockStatement body)
    {
        if(!TryGetTypeInformation(type, out var info)) 
            throw new BinderTypeScopeExeception($"{AddMethodBody} - compiler bug, no info!");
        info.MethodBodies.Add(func, body);
    }

    public void AddMember(TypeSymbol type, MemberSymbol member)
    {
        if(!TryGetTypeInformation(type, out var info)) 
            throw new BinderTypeScopeExeception($"{AddMember} - compiler bug, no info!");
        info.Members.Add(member);
    }

    public bool TryFindMember(TypeSymbol type, string name, [NotNullWhen(true)] out MemberSymbol? memberSymbol)
    {
        //TODO: other generics objects?
        memberSymbol = null;
        var typeInfo = GetTypeAndInformation(type);
        if(typeInfo is null) return false;

        var members = typeInfo.Value.Info.Members;
        memberSymbol = members.FirstOrDefault(m => m.Name == name);
        if(memberSymbol is null && type is ListTypeSymbol)
        {   //Try again if it's a list - incase it exists on the listbase
            TryFindMember(TypeSymbol.ListBase, name, out memberSymbol);
        }
        return memberSymbol is not null;
    }

    public bool IsSpecializedAs(TypeSymbol param, TypeSymbol concrete)
    {
        return TryGetTypeInformation(param, out var typeInfo) 
            && typeInfo.SpecializedAs is not null
            && concrete==typeInfo.SpecializedAs;
    }

    public TypeMemberInformation GetTypeMemberInformation()
    {
        if(_scopes.Count != 1) 
            throw new BinderTypeScopeExeception($"Illegal state, expected 1 scope, but had {_scopes.Count}");
        
        var tmpMembers = new TypeFieldDict();
        var tmpFuncs = new TypeMethodDict();

        foreach(var (_, info) in _scopes[0])
        {
            tmpMembers.Add(info.Type, info.Members);
            tmpFuncs.Add(info.Type, info.MethodBodies);
        }

        var members = new ReadOnlyDictionary<TypeSymbol, IList<MemberSymbol>>(tmpMembers);
        var funcs = new ReadOnlyDictionary<TypeSymbol, Dictionary<FunctionSymbol, BoundBlockStatement>>(tmpFuncs);
        var declaredTypes = _userDefinedTypes.ToList().AsReadOnly();
        return new TypeMemberInformation(members, funcs, declaredTypes);
    }
}