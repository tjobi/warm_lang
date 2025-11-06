using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
        public BinderTypeScopeExeception(string msg) : base(msg) { }
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
        yield return TypeSymbol.Func;
    }

    private readonly List<InternalTypeScope> _scopes;
    private readonly HashSet<TypeSymbol> _userDefinedTypes;
    private readonly ErrorWarrningBag _diag;

    private InternalTypeScope Top => _scopes[^1];
    private InternalTypeScope Global => _scopes[0];

    private TypeInformationLookup _idToInformation;

    private UnionFind _typeUnion;

    private static int TypeToId(TypeSymbol t) => t.ID;

    public BinderTypeScope(ErrorWarrningBag diag)
    {
        _diag = diag;
        _typeUnion = new(TypeToId);
        _scopes = new();
        _idToInformation = new();
        _userDefinedTypes = new();
        _scopes.Add(new InternalTypeScope());
        foreach (var builtin in BuiltinTypes())
        {
            var id = TypeToId(builtin);
            Global.Add(builtin.Name, id);
            ImmutableArray<TypeSymbol>? typeParams = null;
            if (builtin == TypeSymbol.List)
            {
                typeParams = ImmutableArray.Create(new TypeSymbol("T"));
            }
            _idToInformation.Add(id, new(builtin, typeParameters: typeParams));
            _typeUnion.Add(builtin);
        }

        foreach (var (type, fields) in BuiltinMembers.CreateMembersForBuiltins())
        {
            _idToInformation[TypeToId(type)] = new TypeInformation(type, fields.ToList(), new());
        }

        foreach (var bulitin in BuiltInFunctions.GetBuiltInFunctions())
        {
            var id = TypeToId(bulitin.Type);
            var info = new FunctionTypeInformation(bulitin.Type, bulitin.Parameters.Select(p => p.Type).ToList(), bulitin.ReturnType);
            _idToInformation.Add(id, info);
            _typeUnion.Add(info.Type);
            Global.Add(bulitin.Type.Name, id);
        }
    }

    public void Push() => _scopes.Add(new());
    public InternalTypeScope Pop()
    {
        var popped = _scopes[^1];
        _scopes.RemoveAt(_scopes.Count - 1);
        return popped;
    }

    public TypeMemberInformation ToProgramTypeMemberInformation()
    {
        if (_scopes.Count != 1)
            throw new BinderTypeScopeExeception($"Illegal state, expected 1 scope, but had {_scopes.Count}");

        var declaredTypes = _userDefinedTypes.ToList().AsReadOnly();
        var typeInfoDict = new Dictionary<TypeSymbol, TypeInformation>();

        foreach (var (id, info) in _idToInformation)
        {
            var originalType = info.Type;
            if (originalType == TypeSymbol.Error || originalType == TypeSymbol.Null) continue;
            var unifiedInfo = _idToInformation[_typeUnion.Find(id)];
            typeInfoDict.Add(originalType, unifiedInfo);
        }
        return new TypeMemberInformation(typeInfoDict.AsReadOnly(), declaredTypes);
    }


    private InternalTypeScope? GetDefiningScopeOf(TypeSymbol? type)
    {
        if (type is not null)
        {
            foreach (var scope in _scopes)
            {
                if (scope.ContainsKey(type.Name))
                    return scope;
            }
        }
        return null;
    }

    private (TypeSymbol Type, TypeInformation Info)? GetTypeAndInformation(TypeSymbol? type, bool ignoreScoping = false)
    {
        if (type is null) return null;

        int typeId = TypeToId(type);
        if (!ignoreScoping)
        {
            var scope = GetDefiningScopeOf(type);
            if (scope is null || type is null) return null;
            typeId = scope[type.Name];
        }
        var parentId = _typeUnion.Find(typeId);
        if (_idToInformation.TryGetValue(parentId, out var info)) return (info.Type, info);
        return null;
    }

    public TypeSymbol GetOrCreateListType(TypeSymbol typeParam)
    {
        // var typeParamParent = _typeUnion.Find(typeParam);
        if (!_typeUnion.TryFind(typeParam, out var typeParamParent))
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
        if (cached is not null) return cached.Type;

        //Too many strings?
        var listType = new TypeSymbol($"{TypeSymbol.List}{_idToInformation.Count}<{typeParam}>");
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
        if (aInfo is ListTypeInformation la && bInfo is ListTypeInformation lb)
            return TypeEquality(la.NestedType, lb.NestedType);
        if (aInfo is FunctionTypeInformation f1 && bInfo is FunctionTypeInformation f2 && f1.Parameters.Count == f2.Parameters.Count)
        {
            for (int i = 0; i < f1.Parameters.Count; i++)
            {
                if (!TypeEquality(f1.Parameters[i], f2.Parameters[i])) return false;
            }
            return TypeEquality(f1.ReturnType, f2.ReturnType);
        }
        return parA == parB;
    }

    public bool TryAddType(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? res,
                           TypeInformation? typeInfo = null,
                           ImmutableArray<TypeSymbol>? typeParameters = null)
    {
        res = null;
        if (GetTypeInformation(type) is not null) return false;
        Top.Add(type.Name, TypeToId(type));

        //So TypeParameters have already been added to _idToInformation
        //  so if there exists something at this id - let's keep it?
        if (_idToInformation.TryGetValue(TypeToId(type), out var existing))
        {
            res = existing.Type;
        }
        else
        {
            typeInfo = _idToInformation[TypeToId(type)] = typeInfo ?? new TypeInformation(type, typeParameters: typeParameters);
            _typeUnion.Add(type);
            res = type;
            if (typeInfo is not TypeParamaterInformation or GenericTypeInformation) _userDefinedTypes.Add(res);
        }
        return true;
    }

    public bool TryAddType(TypeSyntaxNode typeSyntax, [NotNullWhen(true)] out TypeSymbol? res,
                           TypeInformation? typeInfo = null,
                           ImmutableArray<TypeSymbol>? typeParameters = null)
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
        if (res is null) return null;
        return res.Value.Type;
    }

    public TypeSymbol GetTypeOrThrow(TypeSyntaxNode? type) => GetTypeOrThrow(AsTypeSymbol(type));
    public TypeSymbol GetTypeOrThrow(TypeSymbol? type)
        => GetType(type) ?? throw new BinderTypeScopeExeception($"{nameof(BinderTypeScope)} - cannot find '{type}'");

    public TypeSymbol GetTypeOrErrorType(TypeSyntaxNode? type)
    {
        if (type is null) return TypeSymbol.Void;
        if (!TryGetType(type, out var res))
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
            TypeSyntaxParameterType tv => new TypeSymbol(tv.Name),
            TypeSyntaxTypeApplication ta => GetOrCreateTypeSymbolFromApplication(ta),
            BadTypeSyntax => TypeSymbol.Error,
            _ => throw new NotImplementedException($"{nameof(BinderTypeScope)}.{nameof(AsTypeSymbol)} doesn't know {typeSyntax}")
        };
    }

    private bool ContainsTypeParameterWithName(TypeSyntaxIdentifier ident, [NotNullWhen(true)] out TypeSymbol? t)
    {
        t = null;
        if (!TryGetTypeInformation(new TypeSymbol(ident.Name), out var info) && info is not TypeParamaterInformation)
            return false;
        t = info.Type;
        return true;
    }

    //Something else that can be done?
    private static TypeSymbol TypeSymbolFromString(string s) => new(s);

    public List<(TypeSymbol Type, IEnumerable<FunctionSymbol> MemberFuncs)> GetFunctionMembers()
    {
        var res = new List<(TypeSymbol, IEnumerable<FunctionSymbol>)>();
        foreach (var scope in _scopes)
        {
            foreach (var (_, typeId) in scope)
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
        if (res is null) return null;
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
        if (!TryGetTypeInformation(type, out var info))
            throw new BinderTypeScopeExeception($"{nameof(AddMethodBody)} - compiler bug, no info!");
        info.MethodBodies.Add(func, body);
    }

    public void AddMember(TypeInformation typeInfo, MemberSymbol member)
    {
        if (typeInfo is GenericTypeInformation gt && member is MemberFuncSymbol f)
        {
            if (gt.IsPartiallyConcrete) throw new NotImplementedException($"Don't allow on partially generic! '{f}'");
            else if (!gt.IsFullyConcrete)
            {
                if (TryGetTypeInformation(gt.SpecializedFrom, out var baseinfo)) typeInfo = baseinfo;
                else throw new BinderTypeScopeExeception($"{nameof(AddMember)} - compiler bug, incorrect assumption");
            }
        }
        typeInfo.Members.Add(member);
    }

    public void AddMember(TypeSymbol type, MemberSymbol member)
    {
        if (!TryGetTypeInformation(type, out var info))
            throw new BinderTypeScopeExeception($"{nameof(AddMember)} - compiler bug, no info!");
        AddMember(info, member);
    }

    public bool TryFindMember(TypeSymbol type, string name, [NotNullWhen(true)] out MemberSymbol? memberSymbol)
    {
        //TODO: other generics objects?
        memberSymbol = null;
        var typeAndInfo = GetTypeAndInformation(type);
        if (typeAndInfo is not var (t, info)) return false;
        var members = typeAndInfo.Value.Info.Members;
        memberSymbol = members.FirstOrDefault(m => m.Name == name);

        // if the type is specialized/instantiation of a generic - member may reside on base
        if (memberSymbol is null && info is GenericTypeInformation gt)
        {
            TryFindMember(gt.SpecializedFrom, name, out memberSymbol);
        }
        return memberSymbol is not null;
    }

    public bool IsSubscriptable(TypeSymbol type, [NotNullWhen(true)] out TypeSymbol? nested)
    {
        //I will just assume no type information means no --
        var info = GetTypeInformation(type);
        nested = null;
        if (type == TypeSymbol.String)
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
        if (info is { Info: ListTypeInformation gt } && gt.SpecializedFrom == TypeSymbol.List)
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
        stack.Push((a, b));
        while (stack.Count > 0)
        {
            (a, b) = stack.Pop();
            var aInfo = _idToInformation[_typeUnion.Find(a)];
            var bInfo = _idToInformation[_typeUnion.Find(b)];

            switch ((aInfo, bInfo))
            {
                //Do nothing if in the default - we have hit terminals - actual types?
                default: break;

                case (PlaceHolderInformation pa, PlaceHolderInformation pb):
                    if (pa.Depth > pb.Depth) _typeUnion.Union(b, a);
                    else _typeUnion.Union(a, b);
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

                case (GenericTypeInformation gt1, GenericTypeInformation gt2)
                    when gt1.SpecializedFrom == gt2.SpecializedFrom: //TODO: TypeEquality here?
                    foreach (var (t1, t2) in gt1.TypeArguments.Zip(gt2.TypeArguments))
                    {
                        stack.Push((t1, t2));
                    }
                    break;
                case (FunctionTypeInformation f1, FunctionTypeInformation f2):
                    //TODO: type arguments
                    if (f1.Parameters.Count == f2.Parameters.Count)
                    {
                        for (int i = 0; i < f1.Parameters.Count; i++) stack.Push((f1.Parameters[i], f2.Parameters[i]));
                        stack.Push((f1.ReturnType, f2.ReturnType));
                    }
                    break;
            }
        }
        /* FIXME: 
            The idea is to catch cases like List<P0> List<List<int>>
            Here we could essentially just union(List<P0>, List<List<int>>)
            So any Find(List<P0>) gives us exactly List<List<int>>.
        */
        if (direction.HasValue)
        {
            //if direction means we union a side to b
            if (direction.Value)
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
        if (baseInfo is null)
        {
            _diag.ReportTypeNotFound(genericType.Name, location!);
            return TypeSymbol.Error;
        }
        if (TypeEquality(baseInfo.Type, TypeSymbol.Func))
        {
            var parameters = typeArguments.Take(typeArguments.Count - 1).ToImmutableArray();
            var (type, _) = CreateFunctionType(parameters, typeArguments[^1]);
            return type;
        }
        if (baseInfo is { TypeParameters: null })
        {
            _diag.ReportNonGenericType(genericType.Name, location);
            return TypeSymbol.Error;
        }
        var baseTypeParameters = baseInfo.TypeParameters.Value;
        if (baseTypeParameters.Length != typeArguments.Count)
        {
            var genericTypeName = $"{genericType}<{string.Join(",", baseTypeParameters.Select(t => t.Name))}>";
            _diag.ReportGenericTypeMismatchingTypeArguments(genericTypeName, typeArguments.Count, baseTypeParameters.Length, location);
            return TypeSymbol.Error;
        }

        var typeArgs = new List<TypeSymbol>();
        var translation = new Dictionary<string, TypeSymbol>();
        var cntConcreteTypeParams = baseTypeParameters.Length;

        for (int i = 0; i < baseTypeParameters.Length; i++)
        {
            var arg = typeArguments[i];
            TypeSymbol argType;
            if (TryGetTypeInformation(arg, out var argInfo)) argType = argInfo.Type;
            else
            {
                _diag.ReportTypeNotFound(arg.Name, location);
                argType = TypeSymbol.Error;
            }

            if (argType == TypeSymbol.Void)
            {
                _diag.ReportTypeIllegalVoidTypeArgument(baseInfo.Type, location);
                return TypeSymbol.Error;
            }

            typeArgs.Add(argType);
            var paramType = baseTypeParameters[i];
            translation.Add(paramType.Name, argType);
            if (argInfo is TypeParamaterInformation) cntConcreteTypeParams--;
        }
        //FIXME - could we move away from creating so many unecessary strings?
        var existing = _idToInformation
                       .Where(kv => kv.Value is GenericTypeInformation)
                       .Select(kv => (GenericTypeInformation)kv.Value)
                       .FirstOrDefault(gt => gt.SpecializedFrom == baseInfo.Type && gt.TypeArguments.SequenceEqual(typeArgs));
        if (existing is not null) return existing.Type;

        var typeName = $"{baseInfo.Type}{_idToInformation.Count}<{string.Join(",", typeArgs)}>";
        var taType = new TypeSymbol(typeName);
        var members = new List<MemberSymbol>();
        var typeInfo = new GenericTypeInformation(taType, baseInfo.Type,
                                                          typeArgs, cntConcreteTypeParams,
                                                          members,
                                                          typeParameters: baseInfo.TypeParameters);
        AddType(typeInfo, AddTo.Global);

        foreach (var member in baseInfo.Members)
        {
            //TODO: Fix methods too... Should be very similar to regular generic functions?
            if (member is MemberFuncSymbol) continue;
            var concreteMemberType = MakeConcrete(member.Type, translation, location);
            members.Add(new MemberFieldSymbol(member.Name, concreteMemberType, member.IsReadOnly, member.IsBuiltin));
        }

        return taType;
    }

    public TypeSymbol MakeConcrete(TypeSymbol param, Dictionary<string, TypeSymbol> concreteOf, WarmLangLexerParser.TextLocation location)
    {
        if (concreteOf.ContainsKey(param.Name)) return concreteOf[param.Name];
        return _idToInformation[TypeToId(param)] switch
        {
            TypeParamaterInformation { Type: var tp } => concreteOf[tp.Name],
            ListTypeInformation lst =>
                GetOrCreateListType(MakeConcrete(lst.NestedType, concreteOf, location)),
            GenericTypeInformation gt =>
                GetOrCreateTypeApplication(gt.SpecializedFrom, gt.TypeArguments.Select(t => MakeConcrete(t, concreteOf, location)).ToList(), location),
            FunctionTypeInformation f =>
                CreateFunctionType(f.Parameters.Select(t => MakeConcrete(t, concreteOf, location)).ToList(),
                                   MakeConcrete(f.ReturnType, concreteOf, location)).Item1,
            null => throw new Exception($"{nameof(MakeConcrete)} - tried to concretify '{param}' with no information"),
            _ => param
        };
    }

    //Actual as in - that one it has been unified with!
    public TypeSymbol GetActualType(TypeSymbol t)
    {
        if (!_typeUnion.TryFind(t, out var res))
            throw new BinderTypeScopeExeception($"Compiler bug - retrieving parent of unknown '{t}'");
        var found = _idToInformation[res];
        return found.Type;
    }

    public void PrintUnion()
    {
        var typeInfo = _idToInformation
                       .Select(kv => kv.Value)
                       .Select(i => (i.Type.Name, i.Type.ID, _typeUnion.Find(i.Type), i.GetType().Name));
        Console.WriteLine("  " + string.Join("\n  ", typeInfo));
    }

    public ImmutableArray<TypeSymbol> CreateTypeParameterSymbols(IList<TypeSyntaxParameterType> syntaxParameterTypes)
    {
        var typeParams = ImmutableArray.CreateBuilder<TypeSymbol>(syntaxParameterTypes.Count);
        foreach (var typeParam in syntaxParameterTypes)
        {
            var type = new TypeSymbol(typeParam.Name);
            _idToInformation[TypeToId(type)] = new TypeParamaterInformation(type, typeParam.Location);
            _typeUnion.Add(type);
            typeParams.Add(type);
        }
        return typeParams.MoveToImmutable();
    }

    public bool AddTypeParametersToScope(IEnumerable<TypeSymbol> typeParameters)
    {
        bool anyFailed = false;
        foreach (var typeParam in typeParameters)
        {
            var location = WarmLangLexerParser.TextLocation.EmptyFile;
            TypeParamaterInformation? tpInfo = null;
            if (!_idToInformation.TryGetValue(TypeToId(typeParam), out var info) && info is not TypeParamaterInformation)
            {
                throw new Exception($"Compiler bug - assumption that {nameof(AddTypeParametersToScope)} is only called on TypeParameterInforamation VIOLATED");
            }
            else tpInfo = (TypeParamaterInformation)info;

            if (!TryAddType(tpInfo.Type, out var _, typeInfo: tpInfo))
            {
                _diag.ReportTypeAlreadyDeclared(tpInfo.Location, typeParam.Name);
                anyFailed = true;
            }
        }
        return anyFailed;
    }

    public ImmutableArray<ParameterSymbol> CreateParameterSymbols(
        IList<(TypeSyntaxNode, WarmLangLexerParser.SyntaxToken)> parsedParameters,
        bool treatNullAsPlaceholder = false
    )
    {
        return CreateParameterSymbols(parsedParameters, out _, treatNullAsPlaceholder);
    }

    private ImmutableArray<ParameterSymbol> CreateParameterSymbols(
        IList<(TypeSyntaxNode, WarmLangLexerParser.SyntaxToken)> parsedParameters,
        out bool anyNonNulls, 
        bool treatNullAsPlaceholder = false
    )
    {
        anyNonNulls = false;
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
        var uniqueParameterNames = new HashSet<string>();

        for (int i = 0; i < parsedParameters.Count; i++)
        {
            var (type, name) = parsedParameters[i];
            
            anyNonNulls =  anyNonNulls || type is not null;
            var paramType = type is null && treatNullAsPlaceholder ? CreatePlacerHolderType() : GetTypeOrErrorType(type);

            //missing names have been reported by the parser
            var paramName = name.Name ?? "NO_NAME";
            if (uniqueParameterNames.Contains(paramName))
            {
                _diag.ReportParameterDuplicateName(name);
            }
            else
            {
                parameters.Add(new ParameterSymbol(paramName, paramType, i));
            }
        }
        return parameters.ToImmutable();
    }

    public (TypeSymbol, FunctionTypeInformation) CreateFunctionType(
        ImmutableArray<ParameterSymbol> parameters,
        TypeSymbol returnType,
        ImmutableArray<TypeSymbol>? typeParameters = null,
        bool isMemberFunc = false
    )
    {
        var parameterTypes = parameters.Select(p => p.Type).ToList();
        return CreateFunctionType(parameterTypes, returnType, typeParameters, isMemberFunc);
    }
    public (TypeSymbol, FunctionTypeInformation) CreateFunctionType(
        IList<TypeSymbol> parameterTypes,
        TypeSymbol returnType,
        ImmutableArray<TypeSymbol>? typeParameters = null,
        bool isMemberFunc = false
    )
    {
        //TODO: So many types and strings are gonna be around
        var sb = new StringBuilder().Append('(');
        if (typeParameters is not null)
        {
            sb.Append('<').AppendJoin(", ", typeParameters.Value).Append('>');
        }

        sb.Append('(')
          .AppendJoin(", ", parameterTypes)
          .Append(") => ")
          .Append(returnType)
          .Append(')');
        var typeName = sb.ToString();
        if (Global.TryGetValue(typeName, out var id))
        {
            if (_idToInformation[id] is FunctionTypeInformation fi) return (fi.Type, fi);
            else throw new BinderTypeScopeExeception($"Compiler bug - {nameof(CreateFunctionType)}, {typeName} is not a function(?)");
        }
        var typeSymbol = new TypeSymbol(sb.ToString());

        var typeInfo = new FunctionTypeInformation(typeSymbol, parameterTypes, returnType, typeParameters, isMemberFunc);
        AddType(typeInfo, AddTo.Global);
        return (typeInfo.Type, typeInfo);
    }

    //tries to infer the type from the expected type, but it is not always possible to do so... i.e. explicitly typed parameters
    public (TypeSymbol, FunctionTypeInformation) CreateLambdaFunctionType(
        WarmLangLexerParser.AST.LambdaExpression lambda,
        out ImmutableArray<ParameterSymbol> parameters,
        out TypeSymbol returnType,
        TypeSymbol? expectedType
    )
    {
        parameters = CreateParameterSymbols(lambda.Parameters!, out var anyExplicityTyped, treatNullAsPlaceholder: true);
        returnType = CreatePlacerHolderType();

        //when no expected proceed as normal...
        if (anyExplicityTyped || expectedType is null) return CreateFunctionType(parameters, returnType);
        if (!TryGetTypeInformation(expectedType, out var expectedInfo)
            || expectedInfo is not FunctionTypeInformation fi
            || fi.Parameters.Count != parameters.Length
            )
            return CreateFunctionType(parameters, returnType);

        Unify(returnType, fi.ReturnType);
        returnType = GetActualType(returnType);
        var paramTypes = new List<TypeSymbol>(parameters.Length);
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = fi.Parameters[i];
            Unify(parameters[i].Type, paramType);
            paramTypes.Add(GetActualType(parameters[i].Type));
        }
        return CreateFunctionType(paramTypes, returnType);
    }

    private enum AddTo
    {
        None, Global, Top, Both
    }

    private void AddType(TypeInformation info, AddTo target, TypeSymbol? type = null)
    {
        type ??= info.Type;
        var id = TypeToId(type);
        _idToInformation[id] = info;
        _typeUnion.Add(type);
        switch (target)
        {
            case AddTo.None: return;
            case AddTo.Both:
                Global.Add(type.Name, id);
                Top.Add(type.Name, id);
                return;
            case AddTo.Global:
                Global.Add(type.Name, id);
                return;
            case AddTo.Top:
                Top.Add(type.Name, id);
                return;
        }
    }
    

}