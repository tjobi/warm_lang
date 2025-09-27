using WarmLangCompiler.Symbols;
using WarmLangCompiler.Binding;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using WarmLangLexerParser.ErrorReporting;
using System.Diagnostics.CodeAnalysis;
namespace WarmLangCompiler.ILGen;

public sealed class CilTypeManager
{
    private readonly Dictionary<TypeSymbol, TypeReference> toCILType;

    private record CachedSignature(TypeSymbol Type, string Name, int Arity);
    private readonly Dictionary<CachedSignature, MethodReference> methodCache;
    private readonly Dictionary<int, TypeReference> funcTypeCache;
    private readonly Dictionary<int, TypeReference> actionTypecache;
    private readonly AssemblyDefinition assemblyDef;
    private readonly IReadOnlyDictionary<TypeSymbol, TypeInformation> infoOf;
    private readonly ErrorWarrningBag _diag;
    private readonly AssemblyDefinition mscorlib;

    public ListMethodHelper ListHelper { get; }

    public CilTypeManager(AssemblyDefinition programAssembly,
                          IReadOnlyDictionary<TypeSymbol, TypeInformation> infoOf, ErrorWarrningBag diag,
                          AssemblyDefinition mscorlib)
    {
        toCILType = new();
        methodCache = new();
        ListHelper = new(this);
        funcTypeCache = [];
        actionTypecache = [];
        assemblyDef = programAssembly;
        this.infoOf = infoOf;
        _diag = diag;
        this.mscorlib = mscorlib;
    }

    public void Add(TypeSymbol key, TypeReference cilType) => toCILType.Add(key, cilType);

    public TypeDefinition GetTypeDefinition(TypeSymbol type) => GetType(type).Resolve();

    public TypeReference GetType(TypeSymbol type)
    {
        if (toCILType.TryGetValue(type, out var typeRef)) return typeRef;

        var typeInfo = infoOf[type];
        switch (typeInfo)
        {
            default:
                //May happen that an inferred type wasn't in toCILTYPE - let's see if it is there now
                // or then just add it
                if (!toCILType.TryGetValue(typeInfo.Type, out typeRef))
                    throw new NotImplementedException($"{nameof(GetType)} - compiler bug know what to do with {type}");
                return toCILType[type] = typeRef;
            case GenericTypeInformation gt:
                var genericBase = GetType(gt.SpecializedFrom);
                var typeParameters = gt.TypeArguments.Select(GetType).ToArray();
                var genericInstance = genericBase.MakeGenericInstanceType(typeParameters);
                return toCILType[type] = genericInstance;

            case PlaceHolderInformation:
                //the binder never hit this placeholder with a union... so failback to mscorlib.Object
                return toCILType[EmitterTypeSymbolHelpers.CILBaseTypeSymbol];

            case FunctionTypeInformation f:
                var baseFuncType = GetCilFuncType(type, f); ;
                return toCILType[type] = baseFuncType;
        }
    }

    /* Only needs to provide the arity - since I don't know how to access
       the type parameters of the declaring type. 
       Meaning, I don't know how to specify I am looking for List`1<int>.Add(int),
        the one that accepts an int... So, fall back on the length of parameter list.
    */
    public MethodReference GetSpecializedMethod(TypeSymbol type, string name, int arity)
    {
        var signature = new CachedSignature(type, name, arity);
        if (methodCache.ContainsKey(signature)) return methodCache[signature];

        //FIXME: Should we add some sort of check to make sure only valid "GenericTypeInformation" or "ListTypeInformation" goes here?
        var info = infoOf[type];
        //Needs be here because GetType has side effects - it creates TypeReferences ... nasty ;(
        var specializedType = GetType(type);
        TypeReference baseType = infoOf[type] switch
        {
            GenericTypeInformation gt => toCILType[gt.SpecializedFrom],
            FunctionTypeInformation f => GetCilFuncType(type, f),
            _ => toCILType[type]
        };

        MethodReference? genericMethod = null;
        foreach (var method in baseType.Resolve().Methods)
        {
            if (method.Name == name && method.Parameters.Count == arity)
            {
                genericMethod = assemblyDef.MainModule.ImportReference(method);
                break;
            }
        }
        if (genericMethod is null)
            throw new Exception($"'{nameof(CilTypeManager)}.{nameof(GetSpecializedMethod)}' found nothing for '{type}'.'{name}'({arity})");
        //TODO: Figure out why the HasThis is required.
        var specializedMethod = new MethodReference(genericMethod.Name, genericMethod.ReturnType, specializedType)
        {
            HasThis = genericMethod.HasThis
        };
        foreach (var @param in genericMethod.Parameters)
        {
            specializedMethod.Parameters.Add(@param);
        }
        return methodCache[signature] = specializedMethod;
    }

    public MethodReference GetSpecializedConstructor(TypeSymbol type, MethodReference ctor)
    {
        var specializedType = GetType(type);
        var specializedMethod = new MethodReference(ctor.Name, ctor.ReturnType, specializedType)
        {
            HasThis = true
        };
        return specializedMethod;
    }

    public bool IsListType(TypeSymbol type)
    {
        if (type == TypeSymbol.Null || type == TypeSymbol.Error) return false;
        return infoOf[type] is ListTypeInformation;
    }

    public bool TryGetTypeInformation(TypeSymbol type, [NotNullWhen(true)] out TypeInformation? result)
    {
        return infoOf.TryGetValue(type, out result);
    }
    
    public TypeSymbol GetActualTypeOrThrowCompilerBug(TypeSymbol type)
    {
        if (!infoOf.TryGetValue(type, out var info))
            throw new Exception($"{nameof(CilTypeManager)} - compiler bug, couldn't find info of {type}");
        return info.Type;
    }

    private TypeReference GetCilFuncType(TypeSymbol type, FunctionTypeInformation func)
    {
        IEnumerable<TypeSymbol>? genericArgs = null;
        TypeReference? res;
        var returnInfo = infoOf[func.ReturnType];
        if (returnInfo.Type == TypeSymbol.Void)
        {
            var isGeneric = func.Parameters.Count > 0;
            if (isGeneric) genericArgs = func.Parameters;
            if (!actionTypecache.TryGetValue(func.Parameters.Count, out res))
            {
                var actionTypeString = isGeneric
                                       ? ("System.Action`" + func.Parameters.Count)
                                       : "System.Action";
                var baseActionType = mscorlib.Modules
                                            .FirstOrDefault(m => m.GetType(actionTypeString) is not null)?
                                            .GetType(actionTypeString);

                if (baseActionType is null)
                    throw new NotImplementedException($"Compiler bug - couldn't retrieve {actionTypeString} type from mscorlib");
                res = actionTypecache[func.Parameters.Count] = assemblyDef.MainModule.ImportReference(baseActionType);
            }
        }
        else
        {
            genericArgs = func.Parameters.Append(func.ReturnType);
            if (!funcTypeCache.TryGetValue(func.Parameters.Count, out res))
            {
                var funcTypeString = "System.Func`" + (func.Parameters.Count + 1);
                var baseFuncType = mscorlib.Modules
                                            .FirstOrDefault(m => m.GetType(funcTypeString) is not null)?
                                            .GetType(funcTypeString);
                if (baseFuncType is null)
                    throw new NotImplementedException($"Compiler bug - couldn't retrieve {funcTypeString} type from mscorlib");
                res = funcTypeCache[func.Parameters.Count] = assemblyDef.MainModule.ImportReference(baseFuncType);
            }
        }
        if (genericArgs is not null)
        {
            res = res.MakeGenericInstanceType(genericArgs.Select(GetType).ToArray());
        }
        return res;
    }
}
public class ListMethodHelper
{
    private readonly CilTypeManager manager;

    public ListMethodHelper(CilTypeManager manager)
    {
        this.manager = manager;
    }

    public MethodReference Empty(TypeSymbol t)
    {
        //I mean, it looks unsafe - but we have type checked the program ...
        return manager.GetSpecializedMethod(t, ".ctor", 0);
    }

    private MethodReference GetMethod(TypeSymbol t, string name, int arity)
    {
        return manager.GetSpecializedMethod(t, name, arity);
    }

    public MethodReference Add(TypeSymbol t) => GetMethod(t, "Add", 1);
    public MethodReference AddMany(TypeSymbol t) => GetMethod(t, "AddRange", 1);
    public MethodReference Remove(TypeSymbol t) => GetMethod(t, "RemoveAt", 1);
    public MethodReference Length(TypeSymbol t) => GetMethod(t, "get_Count", 0);
    public MethodReference Subscript(TypeSymbol t) => GetMethod(t, "get_Item", 1);

    public MethodReference Update(TypeSymbol t) => GetMethod(t, "set_Item", 2);

}