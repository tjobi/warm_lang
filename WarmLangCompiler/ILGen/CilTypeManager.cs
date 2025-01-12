using WarmLangCompiler.Symbols;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using WarmLangLexerParser.ErrorReporting;
namespace WarmLangCompiler.ILGen;

public sealed class CilTypeManager 
{
    private readonly Dictionary<TypeSymbol, TypeReference> toCILType;

    private record CachedSignature(TypeSymbol Type, string Name, int Arity);
    private readonly Dictionary<CachedSignature, MethodReference> methodCache;
    
    private readonly TypeReference genericList;
    private readonly AssemblyDefinition mscorlib, assemblyDef;
    private readonly ErrorWarrningBag _diag;

    public ListMethodHelper ListHelper { get; }

    public CilTypeManager(AssemblyDefinition mscorlib, AssemblyDefinition programAssembly, ErrorWarrningBag diag)
    {
        toCILType = new();
        methodCache = new();
        ListHelper = new(this);
        this.mscorlib = mscorlib;
        assemblyDef = programAssembly;
        _diag = diag;
        var baseList = mscorlib.MainModule.GetType("System.Collections.Generic.List`1");
        genericList = assemblyDef.MainModule.ImportReference(baseList);
    }

    public void Add(TypeSymbol key, TypeReference cilType) => toCILType.Add(key, cilType);
    

    public TypeReference GetType(TypeSymbol key) 
    {
        // key = key.Resolve();
        if(toCILType.ContainsKey(key)) return toCILType[key];

        //If it is not there, then we assume it's a generic type - so create it and cache it
        var typeDef = GetSpecializedTypeDefinition(key);
        return toCILType[key] = typeDef;
    }

    private TypeReference GetSpecializedTypeDefinition(TypeSymbol type)
    {
        if(type is PlaceholderTypeSymbol p && p.ActualType is null)
        {
            throw new Exception("Cannot emit placeholder type that has no actual type!");
        }
        if(type is not ListTypeSymbol l) 
        {
            var msg = $"'{nameof(CilTypeManager)}.{nameof(GetSpecializedTypeDefinition)}' doesn't know type of '{type}'";
            throw new NotImplementedException(msg);
        }
        var inner = GetType(l.InnerType);
        return genericList.MakeGenericInstanceType(inner);
    }

    /* Only needs to provide the arity - since I don't know how to access
       the type parameters of the declaring type. 
       Meaning, I don't know how to specify I am looking for List`1<int>.Add(int),
        the one that accepts an int... So, fall back on the length of parameter list.
    */
    public MethodReference GetSpecializedMethod(TypeSymbol type, string name, int arity)
    {
        var signature = new CachedSignature(type, name, arity);
        if(methodCache.ContainsKey(signature)) return methodCache[signature];

        if(type is not ListTypeSymbol l) 
        {
            var msg = $"'{nameof(CilTypeManager)}.{nameof(GetSpecializedTypeDefinition)}' doesn't know type of '{type}'";
            throw new NotImplementedException(msg);
        }
        
        MethodReference? genericMethod = null;
        foreach(var method in genericList.Resolve().Methods)
        {
            if(method.Name == name && method.Parameters.Count == arity)
            {
                genericMethod = assemblyDef.MainModule.ImportReference(method);
                break;
            }
        }
        if(genericMethod is null) 
            throw new Exception($"'{nameof(CilTypeManager)}.{nameof(GetSpecializedTypeDefinition)}' found nothing for '{type}'.'{name}'({arity})");
        var specializedType = GetType(type);
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

    public MethodReference GetConstructor(TypeSymbol type, int arity = 0) 
        => GetSpecializedMethod(type, ".ctor", arity);
}
public class ListMethodHelper
{
    private readonly CilTypeManager manager;

    public ListMethodHelper(CilTypeManager manager)
    {
        this.manager = manager;
    }

    private ListTypeSymbol EnsureList(TypeSymbol t)
    {
        if(t is not ListTypeSymbol l) 
            throw new Exception($"{nameof(ListMethodHelper)} was passed a non-list type!");
        return l;
    }

    public MethodReference Empty(TypeSymbol t)
    {
        EnsureList(t);
        return manager.GetConstructor(t);
    }

    private MethodReference GetMethod(TypeSymbol t, string name, int arity)
    {
        EnsureList(t);
        return manager.GetSpecializedMethod(t, name, arity);
    }

    public MethodReference Add(TypeSymbol t) => GetMethod(t, "Add", 1);
    public MethodReference AddMany(TypeSymbol t) => GetMethod(t, "AddRange", 1);
    public MethodReference Remove(TypeSymbol t) => GetMethod(t, "RemoveAt", 1);
    public MethodReference Length(TypeSymbol t) => GetMethod(t, "get_Count", 0);
    public MethodReference Subscript(TypeSymbol t) => GetMethod(t, "get_Item", 1);

    public MethodReference Update(TypeSymbol t) => GetMethod(t, "set_Item", 2);

}