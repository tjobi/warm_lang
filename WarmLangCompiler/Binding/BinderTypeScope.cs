using System.Diagnostics.CodeAnalysis;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST.TypeSyntax;

namespace WarmLangCompiler.Binding;

using InternalTypeScope = Dictionary<string, int>;
using TypeInformationLookup = Dictionary<int, TypeInformation>;

public sealed class BinderTypeScope
{
    private class BinderTypeScopeExeception : Exception 
    {
        public BinderTypeScopeExeception(string msg) : base(msg) {}
    }

    private static IEnumerable<TypeSymbol> BuiltinTypes() 
    {
        yield return TypeSymbol.Error;
        yield return TypeSymbol.Null;
        yield return TypeSymbol.Void;
        yield return TypeSymbol.Int;
        yield return TypeSymbol.Bool;
        yield return TypeSymbol.String;
        yield return TypeSymbol.List;
    }

    private readonly List<InternalTypeScope> _scopes;
    private readonly HashSet<TypeSymbol> _userDefinedTypes;
    private InternalTypeScope Top => _scopes[^1];
    private InternalTypeScope Global => _scopes[0];

    private TypeInformationLookup _idToInformation;

    private UnionFind _typeUnion;
    
    private static int TypeToId(TypeSymbol t) => t.TypeID;

    public BinderTypeScope()
    {
        _typeUnion = new(TypeToId);
        _scopes = new();
        _idToInformation = new();
        _userDefinedTypes = new();
        _scopes.Add(new InternalTypeScope());
        foreach(var builtin in BuiltinTypes()) 
        {
            Global.Add(builtin.Name, TypeToId(builtin));
            _idToInformation.Add(TypeToId(builtin), new(builtin));
            _typeUnion.Add(builtin);
        }

        foreach(var (type, fields) in BuiltinMembers.CreateMembersForBuiltins())
        {
            _idToInformation[TypeToId(type)] = new TypeInformation(type, fields.ToList(), new());
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
                if(scope.ContainsKey(type.Name)) 
                    return scope;
            }
        }
        return null;
    }

    private (TypeSymbol Type, TypeInformation Info)? GetTypeAndInformation(TypeSymbol? type)
    {

        var scope = GetDefiningScopeOf(type);
        if(scope is null || type is null) return null;
        var parentId = _typeUnion.Find(scope[type.Name]);
        if(_idToInformation.TryGetValue(parentId, out var info)) return (info.Type, info);
        return null;
    }

    public TypeSymbol GetOrCreateListType(TypeSymbol typeParam)
    {
        var typeParamParent = _typeUnion.Find(typeParam);
        var typeParamInfo = _idToInformation[typeParamParent];
        typeParam = typeParamInfo.Type;
        //TODO: Is this necessary?
        var cached = _idToInformation
                     .Where(kv => kv.Value is ListTypeInformation gt)
                     .Select(kv => kv.Value)
                     .FirstOrDefault(info => info is ListTypeInformation gt 
                                     && gt.SpecializedFrom == TypeSymbol.List 
                                     && gt.NestedType == typeParam);
        if(cached is not null) return cached.Type;

        //Too many strings?
        var listType = new TypeSymbol($"{TypeSymbol.List}<{typeParam}>");
        var info = new ListTypeInformation(listType, TypeSymbol.List, typeParam);
        Global.Add(listType.Name, TypeToId(listType));
        _idToInformation.Add(TypeToId(listType), info);
        _typeUnion.Add(listType);
        return listType;
    }

    public bool TypeEquality(TypeSymbol a, TypeSymbol b)
    {
        // Unify(a,b);
        int parA = _typeUnion.Find(a);
        int parB = _typeUnion.Find(b);
        var aInfo = _idToInformation[parA];
        var bInfo = _idToInformation[parB];
        //FIXME: Couldn't we somehow do this from the unify?
        if(aInfo is ListTypeInformation la && bInfo is ListTypeInformation lb)
            return TypeEquality(la.NestedType, lb.NestedType);
        return parA == parB;
    }

    public bool TryAddType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res, TypeInformation? typeInfo = null)
    {
        res = null;
        var knownTypeOrNull = GetType(type); 
        if(knownTypeOrNull is not null) return false;
        Top.Add(type.Name, TypeToId(type));
        _idToInformation[TypeToId(type)] = typeInfo ?? new TypeInformation(type);
        _typeUnion.Add(type);
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
            foreach(var (_, typeId) in scope) 
            {
                var info = _idToInformation[typeId];
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
        if(memberSymbol is null && info is ListTypeInformation gt)
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
        var typeInfoDict = new Dictionary<TypeSymbol, TypeInformation>();
        foreach(var (_, typeId) in Global)
        {
            var info = _idToInformation[_typeUnion.Find(typeId)];
            if(typeInfoDict.ContainsKey(info.Type)) continue;
            typeInfoDict.Add(info.Type, info);
        }
        return new TypeMemberInformation(typeInfoDict.AsReadOnly(), declaredTypes);
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
        return info is ListTypeInformation { NestedType: TypeSymbol n } && (nested = n) is not null;
    }

    public bool IsListTypeAndGetNested(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? nested)
    {
        
        var info = GetTypeAndInformation(type);
        nested = null;
        if(info is { Info: ListTypeInformation gt } && gt.SpecializedFrom == TypeSymbol.List)
        {
            nested = gt.NestedType;
        }
        return nested is not null;
    }

    public void Unify(TypeSymbol a, TypeSymbol b)
    {
        var _a = a;
        var _b = b;
        var stack = new Stack<(TypeSymbol, TypeSymbol)>();
        //null if nothing, if true then union(a,b) else union(b,a)
        bool? direction = null;
        stack.Push((a,b));
        // Console.WriteLine($"----- unifying ({a},{b}) ----");
        // PrintUnion();
        while(stack.Count > 0)
        {
            (a, b) = stack.Pop();
            var aInfo = _idToInformation[_typeUnion.Find(a)];
            var bInfo = _idToInformation[_typeUnion.Find(b)];

            switch((aInfo, bInfo))
            {
                //Do nothing if in the default - we have hit terminals - actual types?
                default: break;

                case (PlaceHolderInformation pa, PlaceHolderInformation pb):
                if(pa.Depth > pb.Depth) _typeUnion.Union(b,a);
                else _typeUnion.Union(a,b);
                direction = !(pa.Depth > pb.Depth);
                break;

                case (PlaceHolderInformation, _): 
                _typeUnion.Union(a, b);
                direction = true;
                break;

                case (_, PlaceHolderInformation): 
                _typeUnion.Union(b, a);
                direction = false;
                break;

                case (ListTypeInformation ga, ListTypeInformation gb): 
                stack.Push((ga.NestedType, gb.NestedType));
                break;
            }
        }
        /* FIXME: 
            The idea is to catch cases like List<P0> List<List<int>>
            Here we could essentially just union(List<P0>, List<List<int>>)
            So any Find(List<P0>) gives us exactly List<List<int>>.
        */
        if(direction.HasValue)
        {
            //if direction means we union a side to b
            if(direction.Value)
            {
                _typeUnion.Union(_a, _b);
            }
            else
            {
                _typeUnion.Union(_b, _a);
            }
        }
        // Console.WriteLine("After");
        // PrintUnion();
        // Console.WriteLine($"Are {_a} and {_b} the same? " + TypeEquality(_a,_b));
        // Console.WriteLine($"----- finished unifying ({a},{b}) ----");
    }

    public TypeSymbol CreatePlacerHolderType()
    {
        // var ts = new PlaceholderTypeSymbol(_scopes.Count);
        //FIXME: Create the placeholder info at the global level
        var placeholderInfo = new PlaceHolderInformation(_scopes.Count);
        var ts = placeholderInfo.Type;
        Global.Add(ts.Name, TypeToId(ts));
        _typeUnion.Add(ts);
        _idToInformation[TypeToId(ts)] = placeholderInfo;
        return ts;
    }

    public void PrintUnion()
    {
        Console.WriteLine(_typeUnion);
        Console.WriteLine("  " + string.Join("\n  ", _idToInformation.Select(kv => kv.Value.Type).Select(t => (t.Name, t.TypeID))));
    }
}