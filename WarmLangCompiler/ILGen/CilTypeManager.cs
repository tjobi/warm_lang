using WarmLangCompiler.Symbols;
using Mono.Cecil;
using Mono.Cecil.Rocks;
namespace WarmLangCompiler.ILGen;

public sealed class CilTypeManager 
{
    private readonly Dictionary<TypeSymbol, TypeReference> toCILType;

    private readonly Dictionary<CachedSignature, MethodReference> methodCache;
    private readonly TypeReference genericList;
    private readonly AssemblyDefinition mscorlib, assemblyDef;

    public ListMethodHelper ListHelper {get;}

    public CilTypeManager(AssemblyDefinition mscorlib, AssemblyDefinition programAssembly)
    {
        toCILType = new();
        methodCache = new();
        ListHelper = new(this);
        this.mscorlib = mscorlib;
        assemblyDef = programAssembly;

        var baseList = mscorlib.MainModule.GetType("System.Collections.Generic.List`1");
        genericList = assemblyDef.MainModule.ImportReference(baseList);
    }

    public void Add(TypeSymbol key, TypeReference cilType) => toCILType.Add(key, cilType);
    

    public TypeReference GetType(TypeSymbol key) 
    {
        if(toCILType.ContainsKey(key)) return toCILType[key];

        //If it is not there, then we assume it's a generic type - so create it and cache it
        var typeDef = GetSpecializedTypeDefinition(key);
        return toCILType[key] = typeDef;
    }

    private TypeReference GetSpecializedTypeDefinition(TypeSymbol type)
    {
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

    public MethodReference GetConstructor(TypeSymbol type) => GetSpecializedMethod(type, ".ctor", 0);

    /*
        var genericList = _mscorlib.MainModule.GetType("System.Collections.Generic.List`1");
        var genericRef =  _assemblyDef.MainModule.ImportReference(genericList);
        var intRef = genericRef.MakeGenericInstanceType(CilTypeOf(TypeSymbol.Int));
        var genericCtr = GetMethodFromTypeDefinition(genericList, ".ctor", GetCilParamNames());
        var intCtor = new MethodReference(genericCtr.Name, genericCtr.ReturnType, intRef){HasThis = genericCtr.HasThis};

        var genericAdd = genericRef.Resolve().Methods.First(m => m.Name == "Add" && m.Parameters.Count == 1);
        var intAdd = new MethodReference(genericAdd.Name, genericAdd.ReturnType, intRef){HasThis = genericAdd.HasThis};
        intAdd.Parameters.Add(genericAdd.Parameters[0]);


        var l = new VariableDefinition(intRef);
        main.Body.Variables.Add(l);
        foreach(var i in main.Body.Variables)
        {
            Console.WriteLine(i.VariableType);
        }

        var p = main.Body.GetILProcessor();
        p.RemoveAt(main.Body.Instructions.Count-1);
        p.Emit(OpCodes.Newobj, intCtor);
        p.Emit(OpCodes.Stloc, l);

        p.Emit(OpCodes.Ldloc, l);
        p.Emit(OpCodes.Ldc_I4_1);
        p.Emit(OpCodes.Call, intAdd);
        // p.Emit(OpCodes.Pop);
        p.Emit(OpCodes.Ret);
        main.Body.Optimize();

        foreach(var i in main.Body.Instructions)
        {
            Console.WriteLine(i);
        }
    */
    private record CachedSignature(TypeSymbol Type, string Name, int Arity);
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