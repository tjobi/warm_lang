using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Binding;

using InternalTypeScope = Dictionary<TypeSymbol, TypeInformation>;

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
        yield return TypeSymbol.List;
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
        _scopes.Add(new InternalTypeScope());
        foreach(var builtin in BuiltinTypes()) 
            Global.Add(builtin, new(builtin));

        foreach(var (type, fields) in BuiltinMembers.CreateMembersForBuiltins())
        {
            Global[type] = new TypeInformation(type, fields.ToList(), new());
        }
    }

    public void Push() => _scopes.Add(new());
    public InternalTypeScope Pop()
    {
        var popped = _scopes[^1];
        _scopes.RemoveAt(_scopes.Count-1);
        return popped;
    }

    private InternalTypeScope? GetDefiningScopeOf(TypeSymbol? type)
    {
        if(type is not null)
        {
            foreach(var scope in _scopes)
            {
                if(scope.ContainsKey(type)) 
                    return scope;
            }
        }
        return null;
    }

    private (TypeSymbol Type, TypeInformation Info)? GetTypeAndInformation(TypeSymbol? type)
    {
        var scope = GetDefiningScopeOf(type);
        if(scope is null || type is null) return null;
        if(scope.TryGetValue(type, out var info)) return (info.Type, info);
        return null;
    }

    public TypeSymbol GetOrCreateListType(TypeSymbol typeParam)
    {
        //Too many strings?
        var listType = new TypeSymbol($"{TypeSymbol.List}<{typeParam}>");
        if(TryGetTypeInformation(listType, out var info)) 
        {
            //We foudn a cached version of the list
            return info.Type;
        }
        info = new GenericTypeInformation(listType, TypeSymbol.List, typeParam);
        Top.Add(listType, info);
        return listType;
    }

    public bool TryAddType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res, TypeInformation? typeInfo = null)
    {
        res = null;
        var knownTypeOrNull = GetType(type); 
        if(knownTypeOrNull is not null) return false;

        Top.Add(type, typeInfo ?? new TypeInformation(type));
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

    public TypeSymbol GetTypeOrThrow(TypeSyntaxNode? type) => GetTypeOrThrow(AsTypeSymbol(type));
    public TypeSymbol GetTypeOrThrow(TypeSymbol? type) 
        => GetType(type) ?? throw new BinderTypeScopeExeception($"{nameof(BinderTypeScope)} - cannot find '{type}'");

    private TypeSymbol AsTypeSymbol(TypeSyntaxNode? typeSyntax) 
    {
        if (typeSyntax is null) return TypeSymbol.Void; //Really?
        return typeSyntax switch
        {
            TypeSyntaxInt => TypeSymbol.Int,
            TypeSyntaxBool => TypeSymbol.Bool,
            TypeSyntaxString => TypeSymbol.String,
            TypeSyntaxIdentifier ident => 
                ContainsTypeParameterWithName(ident, out var typeParam) ? typeParam : TypeSymbolFromString(ident.Name),
            TypeSyntaxList lst => GetOrCreateListType(AsTypeSymbol(lst.InnerType)),
            TypeSyntaxParameterType tv => new TypeParameterSymbol(tv),
            _ => throw new NotImplementedException($"BinderTypeExntensions doesn't know {typeSyntax}")
        };
    }
    
    private bool ContainsTypeParameterWithName(TypeSyntaxIdentifier ident, [NotNullWhen(true)] out TypeSymbol? t)
    {
        return TryGetType(new TypeParameterSymbol(ident.Name, ident.Location), out t);
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

    public TypeInformation GetTypeInformationOrThrow(TypeSymbol type)
    {
        return GetTypeInformation(type) 
               ?? throw new BinderTypeScopeExeception($"{nameof(GetTypeAndInformation)} - couldn't find information for '{type}'");
    }

    public TypeInformation? GetTypeInformation(TypeSymbol type)
    {
        var res = GetTypeAndInformation(type);
        if(res is null) return null;
        return res.Value.Info;
    }
    public bool TryGetTypeInformation(TypeSymbol type, [NotNullWhen(true)] out TypeInformation? typeInfo)
        => (typeInfo = GetTypeInformation(type)) is not null;

    private TypeInformation UpdateTypeInformation(TypeSymbol type, Func<TypeInformation, TypeInformation> f)
    {
        var scope = GetDefiningScopeOf(type);
        if(scope is null || !scope.ContainsKey(type)) 
            throw new BinderTypeScopeExeception($"{nameof(UpdateTypeInformation)} - compiler bug - where is '{type}' defined?");
        return scope[type] = f(scope[type]);
    }

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
        var typeAndInfo = GetTypeAndInformation(type);
        if(typeAndInfo is not var (t, info)) return false;

        var members = typeAndInfo.Value.Info.Members;
        memberSymbol = members.FirstOrDefault(m => m.Name == name);

        // if the type is specialized/instantiation of a generic - member may reside on base
        if(memberSymbol is null && info is GenericTypeInformation gt)
        {
            TryFindMember(gt.SpecializedFrom, name, out memberSymbol);
        }
        return memberSymbol is not null;
    }

    public TypeMemberInformation GetTypeMemberInformation()
    {
        if(_scopes.Count != 1) 
            throw new BinderTypeScopeExeception($"Illegal state, expected 1 scope, but had {_scopes.Count}");
        
        var declaredTypes = _userDefinedTypes.ToList().AsReadOnly();
        return new TypeMemberInformation(_scopes[0].AsReadOnly(), declaredTypes);
    }

    public bool IsSubscriptable(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? nested)
    {
        //I will just assume no type information means no --
        var info = GetTypeInformation(type);
        nested = null;
        if(type == TypeSymbol.String) 
        {
            nested = TypeSymbol.Int;
            return true;
        }
        return info is GenericTypeInformation { NestedType: TypeSymbol n } && (nested = n) is not null;
    }

    public bool IsListTypeAndGetNested(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? nested)
    {
        var info = GetTypeAndInformation(type);
        nested = null;
        if(info is { Info: GenericTypeInformation gt } && gt.SpecializedFrom == TypeSymbol.List)
        {
            nested = gt.NestedType;
        }
        return nested is not null;
    }

    public void Unify(TypeSymbol _a, TypeSymbol _b)
    {
        
        var stack = new Stack<(TypeSymbol, TypeSymbol)>();
        stack.Push((_a,_b));
        while(stack.Count > 0)
        {
            var (curA,curB) = stack.Peek();
            
            switch(stack.Pop())
            {
            case (PlaceholderTypeSymbol aa, PlaceholderTypeSymbol bb):
                if(aa.ActualType is null && bb.ActualType is null)          
                {
                    if(aa.Depth > bb.Depth) bb.Union(aa);
                    else                    aa.Union(bb);
                }
                else if(aa.ActualType is not null && bb.ActualType is null) bb.Union(aa.ActualType);
                else if(aa.ActualType is null && bb.ActualType is not null) aa.Union(bb.ActualType);
                else //neither are null - failure state
                    throw new BinderTypeScopeExeception($"Something is wrong here - neither '{aa}' nor '{bb}' is null");
                break;
            // case (PlaceholderTypeSymbol aa,_):
            //     var updated = UpdateTypeInformation(aa, info => new(info.Type, curB));
            //     aa.Union(curB);
            //     break;
            // case (_,PlaceholderTypeSymbol bb):
            //     UpdateTypeInformation(bb, info => new(info.Type, curA));
            //     bb.Union(curA);
            //     break;
            case (ListTypeSymbol {InnerType: PlaceholderTypeSymbol ut1}, ListTypeSymbol {InnerType: PlaceholderTypeSymbol ut2}):
                stack.Push((ut1,ut2));
                break;
            case (ListTypeSymbol {InnerType: PlaceholderTypeSymbol ut}, ListTypeSymbol lb):
                stack.Push((ut,lb.InnerType));
                break;
            case (ListTypeSymbol la, ListTypeSymbol {InnerType: PlaceholderTypeSymbol ut}):
                stack.Push((ut, la.InnerType));
                break;
            case (ListTypeSymbol la, ListTypeSymbol lb):
                stack.Push((la.InnerType, lb.InnerType));
                break;
            }
        }
        _a.Resolve();
        _b.Resolve();
    }

    public PlaceholderTypeSymbol CreatePlacerHolderType()
    {
        var ts = new PlaceholderTypeSymbol(_scopes.Count);
        //FIXME: Create the placeholder info at the global level
        Global.Add(ts, new TypeInformation(ts));
        // if(!TryAddType(ts, out var _))
        //     throw new BinderTypeScopeExeception($"{nameof(CreatePlacerHolderType)} couldn't declare placeholder '{ts}'");
        return ts;
    }
}