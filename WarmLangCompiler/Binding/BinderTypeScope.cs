using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Mono.CompilerServices.SymbolWriter;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST.TypeSyntax;
using WarmLangLexerParser.ErrorReporting;

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
    private readonly ErrorWarrningBag _diag;

    private InternalTypeScope Top => _scopes[^1];
    private InternalTypeScope Global => _scopes[0];

    private TypeInformationLookup _idToInformation;

    private UnionFind _typeUnion;
    
    private static int TypeToId(TypeSymbol t) => t.TypeID;

    public BinderTypeScope(ErrorWarrningBag diag)
    {
        _diag = diag;
        _typeUnion = new(TypeToId);
        _scopes = new();
        _idToInformation = new();
        _userDefinedTypes = new();
        _scopes.Add(new InternalTypeScope());
        foreach(var builtin in BuiltinTypes()) 
        {
            var id = TypeToId(builtin);
            Global.Add(builtin.Name, id);
            ImmutableArray<TypeParameterSymbol>? typeParams = null;
            if(builtin == TypeSymbol.List)
            {
                typeParams = ImmutableArray.Create(new TypeParameterSymbol("T", new WarmLangLexerParser.TextLocation(-1,-1)));
            }
            _idToInformation.Add(id, new(builtin, typeParameters: typeParams));
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

    private (TypeSymbol Type, TypeInformation Info)? GetTypeAndInformation(TypeSymbol? type, bool ignoreScoping = false)
    {
        if(type is null) return null;
        
        int typeId = TypeToId(type);
        if(!ignoreScoping)
        {
            var scope = GetDefiningScopeOf(type);
            if(scope is null || type is null) return null;
            typeId = scope[type.Name];
        }
        var parentId = _typeUnion.Find(typeId);
        if(_idToInformation.TryGetValue(parentId, out var info)) return (info.Type, info);
        return null;
    }

    public TypeSymbol GetOrCreateListType(TypeSymbol typeParam)
    {
        // var typeParamParent = _typeUnion.Find(typeParam);
        if(!_typeUnion.TryFind(typeParam, out var typeParamParent))
        {
            throw new BinderTypeScopeExeception($"Compiler bug - couldn't find parent of type parameter '{typeParam}'");
        }
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

    public bool TryAddType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res, 
                           TypeInformation? typeInfo = null, 
                           ImmutableArray<TypeParameterSymbol>? typeParameters = null)
    {
        res = null;
        var knownTypeOrNull = GetTypeInformation(type); 
        if(knownTypeOrNull is not null) 
        {
            return false;
        }
        Top.Add(type.Name, TypeToId(type));
        typeInfo = _idToInformation[TypeToId(type)] = typeInfo ?? new TypeInformation(type, typeParameters: typeParameters);
        _typeUnion.Add(type);
        res = type;
        if(type is not TypeParameterSymbol || typeInfo is GenericTypeInformation) _userDefinedTypes.Add(res);
        return true;
    }

    public bool TryAddType(TypeSyntaxNode typeSyntax, [NotNullWhen(true)] out TypeSymbol? res, 
                           TypeInformation? typeInfo = null,
                           ImmutableArray<TypeParameterSymbol>? typeParameters = null)
        => TryAddType(AsTypeSymbol(typeSyntax), out res, typeInfo, typeParameters);

    public bool ContainsTypeWithNameOf(string name) 
        => GetType(new TypeSymbol(name)) is not null;

    public bool TryGetType(TypeSyntaxNode? type, [NotNullWhen(true)] out TypeSymbol? res) 
        => TryGetType(AsTypeSymbol(type), out res);
    
    public bool TryGetType(TypeSymbol? type, [NotNullWhen(true)] out TypeSymbol? res)
        => (res = GetType(type)) is not null;

    public bool TryGetType(string typeName, [NotNullWhen(true)] out TypeSymbol? res)
        => TryGetType(TypeSymbolFromString(typeName), out res);

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

    public TypeSymbol GetTypeOrErrorType(TypeSyntaxNode? type)
    {
        if(type is null) return TypeSymbol.Void;
        if(!TryGetType(type, out var res))
        {
            _diag.ReportTypeNotFound(type.ToString(), type.Location);
            res = TypeSymbol.Error;
        }
        return res;
    }

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
            TypeSyntaxTypeApplication ta => GetOrCreateTypeSymbolFromApplication(ta),
            _ => throw new NotImplementedException($"{nameof(BinderTypeScope)}.{nameof(AsTypeSymbol)} doesn't know {typeSyntax}")
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

    public bool TryGetTypeInformation(TypeSyntaxNode type, [NotNullWhen(true)] out TypeInformation? typeInfo)
        => TryGetTypeInformation(AsTypeSymbol(type), out typeInfo);

    public bool TryGetTypeInformation(TypeSymbol type, [NotNullWhen(true)] out TypeInformation? typeInfo)
        => (typeInfo = GetTypeInformation(type)) is not null;

    private TypeInformation? GetTypeInformationIgnoringScopingRules(TypeSymbol type)
    {
        var res = GetTypeAndInformation(type, ignoreScoping: true);
        return res?.Info;
    }

    public void AddMethodBody(TypeSymbol type, FunctionSymbol func, BoundBlockStatement body)
    {
        if(!TryGetTypeInformation(type, out var info)) 
            throw new BinderTypeScopeExeception($"{nameof(AddMethodBody)} - compiler bug, no info!");
        info.MethodBodies.Add(func, body);
    }

    public void AddMember(TypeInformation typeInfo, MemberSymbol member)
    {
        if(typeInfo is GenericTypeInformation gt && member is MemberFuncSymbol f)
        {
            if(gt.IsPartiallyConcrete) throw new NotImplementedException($"Don't allow on partially generic! '{f}'");
            else if(!gt.IsFullyConcrete)
            {
                if(TryGetTypeInformation(gt.SpecializedFrom, out var baseinfo)) typeInfo = baseinfo;
                else throw new BinderTypeScopeExeception($"{nameof(AddMember)} - compiler bug, incorrect assumption");
            }
        }
        typeInfo.Members.Add(member);
    }

    public void AddMember(TypeSymbol type, MemberSymbol member)
    {
        if(!TryGetTypeInformation(type, out var info)) 
            throw new BinderTypeScopeExeception($"{nameof(AddMember)} - compiler bug, no info!");
        AddMember(info, member);
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
        var typeInfoDict = new Dictionary<TypeSymbol, TypeInformation>();
    
        foreach(var (id, info) in _idToInformation)
        {
            var originalType = info.Type;
            if(originalType == TypeSymbol.Error || originalType == TypeSymbol.Null ) continue;

            var unifiedInfo = _idToInformation[_typeUnion.Find(id)];
            typeInfoDict.Add(originalType, unifiedInfo);
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

    public TypeSymbol GetOrCreateTypeSymbolFromApplication(TypeSyntaxTypeApplication ta)
    {
        var typeParams = ta.TypeArguments.Select(AsTypeSymbol);
        return GetOrCreateTypeApplication(AsTypeSymbol(ta.GenericType), typeParams.ToList(), ta.Location);
    }

    public TypeSymbol GetOrCreateTypeApplication(TypeSymbol genericType, IList<TypeSymbol> typeArguments, WarmLangLexerParser.TextLocation location)
    {
        var baseInfo = GetTypeInformation(genericType);
        if(baseInfo is null)
        {
            _diag.ReportTypeNotFound(genericType.Name, location!);
            return TypeSymbol.Error;
        }
        if(baseInfo is { TypeParameters: null })
        {
            _diag.ReportNonGenericType(genericType.Name, location);
            return TypeSymbol.Error;
        }
        var baseTypeParameters = baseInfo.TypeParameters.Value;
        if(baseTypeParameters.Length != typeArguments.Count)
        {
            var genericTypeName = $"{genericType}<{string.Join(",", baseTypeParameters.Select(t => t.Name))}>";
            _diag.ReportGenericTypeMismatchingTypeArguments(genericTypeName, typeArguments.Count, baseTypeParameters.Length, location);
            return TypeSymbol.Error;
        }

        var typeArgs = new List<TypeSymbol>();
        var translation = new Dictionary<TypeSymbol, TypeSymbol>();
        var cntConcreteTypeParams = baseTypeParameters.Length;

        for(int i = 0; i < baseTypeParameters.Length ; i++)
        {
            var arg = typeArguments[i];
            TypeSymbol argType;
            if(!TryGetTypeInformation(arg, out var argInfo)) 
            {
                _diag.ReportTypeNotFound(arg.Name, location);
                argType = TypeSymbol.Error;
            } 
            else argType = argInfo.Type;
            
            typeArgs.Add(argType);
            var paramType = baseTypeParameters[i];
            translation.Add(paramType, argType);
            if(argType is TypeParameterSymbol) cntConcreteTypeParams--;
        }
        //FIXME - could we move away from creating so many unecessary strings?
        var typeName = $"{baseInfo.Type}<{string.Join(",", typeArgs)}>";
        if(Global.TryGetValue(typeName, out var taId)) return _idToInformation[taId].Type;

        var taType = new TypeSymbol(typeName);
        taId = TypeToId(taType);

        var members = new List<MemberSymbol>();
        foreach(var member in baseInfo.Members)
        {
            //TODO: Fix methods too... Should be very similar to regular generic functions?
            if(member is MemberFuncSymbol) continue;
            var concreteMemberType = MakeConcrete(member.Type, translation, location);
            members.Add(new MemberFieldSymbol(member.Name, concreteMemberType, member.IsReadOnly, member.IsBuiltin));
        }

        _idToInformation[taId] = new GenericTypeInformation(taType, baseInfo.Type, 
                                                            typeArgs, cntConcreteTypeParams, 
                                                            members,
                                                            typeParameters: baseInfo.TypeParameters);
        Global.Add(taType.Name, taId);
        _typeUnion.Add(taType);
        return taType;
    }

    public TypeSymbol MakeConcrete(TypeSymbol param, Dictionary<TypeSymbol, TypeSymbol> concreteOf, WarmLangLexerParser.TextLocation location) 
    {
        if(concreteOf.ContainsKey(param)) return concreteOf[param];
        return _idToInformation[TypeToId(param)] switch
        {
            TypeInformation { Type: TypeParameterSymbol tp } => concreteOf[tp],
            ListTypeInformation lst => 
                GetOrCreateListType(MakeConcrete(lst.NestedType, concreteOf, location)),
            GenericTypeInformation gt => 
                GetOrCreateTypeApplication(gt.SpecializedFrom, gt.TypeArguments.Select(t => MakeConcrete(t, concreteOf, location)).ToList(), location),
            null => throw new Exception($"{nameof(MakeConcrete)} - tried to concretify '{param}' with no information"),
            _ => param
        };
    }

    public void PrintUnion()
    {
        Console.WriteLine(_typeUnion);
        Console.WriteLine("  " + string.Join("\n  ", _idToInformation.Select(kv => kv.Value.Type).Select(t => (t.Name, t.TypeID))));
    }
}