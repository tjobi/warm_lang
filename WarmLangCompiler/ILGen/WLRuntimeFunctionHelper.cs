namespace WarmLangCompiler.ILGen;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WarmLangCompiler.Symbols;

public class WLRuntimeFunctionHelper
{
    private readonly TypeDefinition _program;
    private readonly MethodReference _stringEqual;
    private readonly MethodReference _toStringConvert, _stringConcat;
    private readonly CilTypeManager typeManager;
    private readonly MethodReference _objEquals;
    private readonly MethodReference _listLength, _listSubscript;
    private readonly TypeReference _listType;
    private readonly bool _debug;

    public MethodDefinition WLToString { get; }
    public MethodDefinition WLEquals { get; }

    public WLRuntimeFunctionHelper(TypeDefinition program,         AssemblyDefinition mscorlib, 
                                   AssemblyDefinition assemblyDef, MethodReference stringEqual, 
                                   MethodReference objEquals,      MethodReference toString, 
                                   MethodReference stringConcat,   CilTypeManager typeManager, 
                                   bool debug = false)
    {
        _program = program;
        _toStringConvert = toString;
        this.typeManager = typeManager;
        _objEquals = objEquals;
        _stringEqual = stringEqual;
        _debug = debug;
        _stringConcat = stringConcat;

        //TODO: This shouldn't exist here.
        // should generate this function for types ... 
        var dotnetICollection = mscorlib.MainModule.GetType("System.Collections.ICollection");
        var dotnetIList = mscorlib.MainModule.GetType("System.Collections.IList");
        _listType = assemblyDef.MainModule.ImportReference(dotnetIList);

        var tmpLength= dotnetICollection
                      .Methods
                      .First(m => m.Name == "get_Count" && m.Parameters.Count == 0);
        _listLength = assemblyDef.MainModule.ImportReference(tmpLength);

        var tmpSubscri = dotnetIList
                        .Methods
                        .First(m => m.Name == "get_Item" && m.Parameters.Count == 1);
        _listSubscript = assemblyDef.MainModule.ImportReference(tmpSubscri);

        WLEquals = CreateWLEquals();
        WLToString = CreateWLToString();
    }
    
    private MethodDefinition CreateWLEquals()
    {
        var wlEqualsDef = new MethodDefinition("__wl_equals", 
                                               MethodAttributes.Public | MethodAttributes.Static, 
                                               typeManager.GetType(TypeSymbol.Bool));
        _program.Methods.Add(wlEqualsDef);
        var dotnetObject = typeManager.GetType(EmitterTypeSymbolHelpers.CILBaseTypeSymbol);
        var arg1 = new ParameterDefinition("arg1", ParameterAttributes.None, dotnetObject);
        var arg2 = new ParameterDefinition("arg2", ParameterAttributes.None, dotnetObject);
        wlEqualsDef.Parameters.Add(arg1);
        wlEqualsDef.Parameters.Add(arg2);

        var body = wlEqualsDef.Body;
        body.InitLocals = true;
        var processor = body.GetILProcessor();
        var returnInstr = processor.Create(OpCodes.Ret);

        // if(arg1 is ArrayList && arg2 is ArrayList)
        //  if(!(arg1.Count == arg2.Count)) return false;    
        //  for(int i = 0; i < arg1.Count; i++)
        //          if( !wlEquals(arg1[i], arg2[i])) return false;
        // if(arg1 is string && arg2 is string)
        //  return string.op_equals(arg1,arg2);
        // return arg1 == arg2;

        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Isinst, _listType);  
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);

        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Isinst, _listType);
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.And);
        var lstBreak = processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        processor.Append(lstBreak);

        // if(arg1.Count != arg2.Count)
        var arg1Count = new VariableDefinition(_listType);
        var arg1asList = new VariableDefinition(_listType);
        var arg2asList = new VariableDefinition(_listType);
        body.Variables.Add(arg1Count);
        body.Variables.Add(arg1asList);
        body.Variables.Add(arg2asList);
        
        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Castclass, _listType);
        processor.Emit(OpCodes.Stloc, arg1asList);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Castclass, _listType);
        processor.Emit(OpCodes.Stloc, arg2asList);

        processor.Emit(OpCodes.Ldloc, arg1asList);
        processor.Emit(OpCodes.Callvirt, _listLength);
        processor.Emit(OpCodes.Stloc, arg1Count);
        processor.Emit(OpCodes.Ldloc, arg1Count);
        processor.Emit(OpCodes.Ldloc, arg2asList);
        processor.Emit(OpCodes.Callvirt, _listLength);
        processor.Emit(OpCodes.Ceq);
        var ifCountMatch = processor.Create(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        processor.Append(ifCountMatch);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Br, returnInstr);

        // if(arg1.count == 0) return true;
        var ldArg1Count = processor.Create(OpCodes.Ldloc, arg1Count);
        processor.Append(ldArg1Count);
        ifCountMatch.Operand = ldArg1Count;
        var ifCountNot0 = processor.Create(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        processor.Append(ifCountNot0);
        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Br, returnInstr);
  
        // for(int i = 0; i < arg1.Count; i++)
        var i = new VariableDefinition(typeManager.GetType(TypeSymbol.Int));
        body.Variables.Add(i);
        var newIValue = processor.Create(OpCodes.Ldc_I4_0);
        processor.Append(newIValue);
        ifCountNot0.Operand = newIValue;
        processor.Emit(OpCodes.Stloc, i);

        var breakToCond = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
        processor.Append(breakToCond);

        // loop body
        // if(!wlEquals(arg1[i], arg2[i])) return false;
        var firstInBody = processor.Create(OpCodes.Ldloc, arg1asList);
        processor.Append(firstInBody);        
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Callvirt, _listSubscript);

        processor.Emit(OpCodes.Ldloc, arg2asList);
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Callvirt, _listSubscript);
        
        processor.Emit(OpCodes.Call, wlEqualsDef);
        var ifTest = processor.Create(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        processor.Append(ifTest);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Br, returnInstr);

        // i++;
        var inc = processor.Create(OpCodes.Ldc_I4_1);
        processor.Append(inc);
        ifTest.Operand = inc;
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Add);
        processor.Emit(OpCodes.Stloc, i);

        // loop condition        
        var condStart = processor.Create(OpCodes.Ldloc, i);
        processor.Append(condStart);
        breakToCond.Operand = condStart;
        processor.Emit(OpCodes.Ldloc, arg1Count);
        processor.Emit(OpCodes.Clt);                    // i < arg1.count
        processor.Emit(OpCodes.Brtrue, firstInBody);    //if (i < arg1.count) goto loopbody;
        processor.Emit(OpCodes.Ldc_I4_1);               
        processor.Emit(OpCodes.Br, returnInstr);        //made it all the way through the loop, return true

        // elseif(arg1 is string ?)
        var strStart = processor.Create(OpCodes.Ldarg, arg1);
        processor.Append(strStart);
        lstBreak.Operand = strStart;
        processor.Emit(OpCodes.Isinst, typeManager.GetType(TypeSymbol.String));
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Isinst, typeManager.GetType(TypeSymbol.String));
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.And);
        var strBreak = processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        processor.Append(strBreak);
        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Call, _stringEqual);
        processor.Emit(OpCodes.Br, returnInstr);
        
        // else primitive equals --uses object.equals();
        var primitiveStart = processor.Create(OpCodes.Ldarg, arg1);
        strBreak.Operand = primitiveStart;
        processor.Append(primitiveStart);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Call, _objEquals);
        processor.Append(returnInstr);

        body.OptimizeMacros();

        if(_debug)
        {
            Console.WriteLine("---- START WL_EQUALS -----");
            foreach(var instr in body.Instructions)
                Console.WriteLine(instr);
            Console.WriteLine("---- END WL_EQUALS -----");
        }

        return wlEqualsDef;
    }

    private MethodDefinition CreateWLToString()
    {
        var wlToString = new MethodDefinition("__wl_tostring", 
                                              MethodAttributes.Public | MethodAttributes.Static, 
                                              typeManager.GetType(TypeSymbol.String));
        _program.Methods.Add(wlToString);
        var dotnetObject = typeManager.GetType(EmitterTypeSymbolHelpers.CILBaseTypeSymbol);
        var arg = new ParameterDefinition("arg", ParameterAttributes.None, dotnetObject);
        wlToString.Parameters.Add(arg);

        var body = wlToString.Body;
        body.InitLocals = true;
        var processor = body.GetILProcessor();
        
        var returnInstr = processor.Create(OpCodes.Ret);
        var convertStart = processor.Create(OpCodes.Ldarg, arg);

        //if(arg is List)
        var outString = new VariableDefinition(typeManager.GetType(TypeSymbol.String));
        body.Variables.Add(outString);

        processor.Emit(OpCodes.Ldarg, arg);
        processor.Emit(OpCodes.Isinst, _listType);
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.Brfalse, convertStart);

        var count = new VariableDefinition(typeManager.GetType(TypeSymbol.Int));
        var asList = new VariableDefinition(_listType);
        body.Variables.Add(count);
        body.Variables.Add(asList);
        processor.Emit(OpCodes.Ldarg, arg);
        processor.Emit(OpCodes.Castclass, _listType);
        processor.Emit(OpCodes.Stloc, asList);
        processor.Emit(OpCodes.Ldloc, asList);
        processor.Emit(OpCodes.Callvirt, _listLength);
        processor.Emit(OpCodes.Stloc, count);

        //string out = "[";
        processor.Emit(OpCodes.Ldstr, "[");
        processor.Emit(OpCodes.Stloc, outString);

        //for(int i = 0; i < arg1.Count; i++)
        var i = new VariableDefinition(typeManager.GetType(TypeSymbol.Int));
        body.Variables.Add(i);
        var loopConditionStart = processor.Create(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Ldc_I4_0);
        processor.Emit(OpCodes.Stloc, i);
        processor.Emit(OpCodes.Br, loopConditionStart);

        //loop body:
        //  outString = outstring + wlToString(elm[i]);
        var firstInBody = processor.Create(OpCodes.Ldloc, outString);
        processor.Append(firstInBody);        
        processor.Emit(OpCodes.Ldloc, asList);
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Callvirt, _listSubscript);
        processor.Emit(OpCodes.Call, wlToString);

        processor.Emit(OpCodes.Call, _stringConcat);  //string.Concat(outString, wlToString(arg[i]));
        processor.Emit(OpCodes.Stloc, outString);     //outstring = ^
        
        // i++;
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Ldc_I4_1);        
        processor.Emit(OpCodes.Add);
        processor.Emit(OpCodes.Stloc, i);

        //STILL in body -> Same as doing if(i < arg.Count - 1) outString += ", ";
        //works by checking the value of i against count AFTER the increment. So if(++i < arg.Count) ...
        processor.Emit(OpCodes.Ldloc, i);
        processor.Emit(OpCodes.Ldloc, count);
        processor.Emit(OpCodes.Clt);
        processor.Emit(OpCodes.Brfalse, loopConditionStart);
        processor.Emit(OpCodes.Ldloc, outString);
        processor.Emit(OpCodes.Ldstr, ", ");
        processor.Emit(OpCodes.Call, _stringConcat);
        processor.Emit(OpCodes.Stloc, outString);

        //loop condition   (i < arg.Count)
        processor.Append(loopConditionStart);
        processor.Emit(OpCodes.Ldloc, count);
        processor.Emit(OpCodes.Clt);                    // i < arg1.count
        processor.Emit(OpCodes.Brtrue, firstInBody);    //if (i < arg1.count) goto loopbody;

        // outString = outString + "]";  
        processor.Emit(OpCodes.Ldloc, outString);
        processor.Emit(OpCodes.Ldstr, "]");
        processor.Emit(OpCodes.Call, _stringConcat);
        processor.Emit(OpCodes.Br, returnInstr);        //made it all the way through the loop, return true

        //else use Convert.ToString(object)
        //It will try to use in order: IConvertible.ToString -> IFormattable.ToString -> object.ToString 
        processor.Append(convertStart);
        processor.Emit(OpCodes.Call, _toStringConvert);
        processor.Append(returnInstr);

        body.OptimizeMacros();

        if(_debug)
        {
            Console.WriteLine("---- START WLToString -----");
            foreach(var instr in body.Instructions)
                Console.WriteLine(instr);
            Console.WriteLine("---- END WLToString -----");
        }

        return wlToString;
    }
}