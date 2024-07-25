namespace WarmLangCompiler.ILGen;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WarmLangCompiler.Symbols;

public class WLRuntimeFunctionHelper
{
    private readonly TypeDefinition _program;
    private readonly Func<TypeSymbol, TypeReference> _cilTypeOf;
    private readonly MethodReference _listLength;
    private readonly MethodReference _listSubscript;
    private readonly MethodReference _stringEqual;
    private readonly MethodReference _debugPrint;
    private readonly MethodReference _toStringConvert;
    private readonly MethodReference _objEquals;

    public WLRuntimeFunctionHelper(TypeDefinition program, Func<TypeSymbol, TypeReference> cilTypeOf, MethodReference listLength, MethodReference listSubscript, MethodReference stringEqual, MethodReference print, MethodReference toString, MethodReference objEquals)
    {
        _program = program;
        _cilTypeOf = cilTypeOf;
        _listLength = listLength;
        _listSubscript = listSubscript;
        _stringEqual = stringEqual;
        _debugPrint = print;
        _toStringConvert = toString;
        _objEquals = objEquals;
    }

        public MethodDefinition CreateWlEquals()
    {
        var wlEqualsDef = new MethodDefinition("__wl_equals", MethodAttributes.Public | MethodAttributes.Static, _cilTypeOf(TypeSymbol.Bool));
        _program.Methods.Add(wlEqualsDef);
        var dotnetObject = _cilTypeOf(EmitterTypeSymbolHelpers.GetCILBaseTypeSymbol());
        var arg1 = new ParameterDefinition("arg1", ParameterAttributes.None, dotnetObject);
        var arg2 = new ParameterDefinition("arg2", ParameterAttributes.None, dotnetObject);
        wlEqualsDef.Parameters.Add(arg1);
        wlEqualsDef.Parameters.Add(arg2);

        var body = wlEqualsDef.Body;
        body.InitLocals = true;
        var processor = body.GetILProcessor();
        var returnInstr = processor.Create(OpCodes.Ret);

        //if(arg1 is ArrayList && arg2 is ArrayList)
        //  if(!(arg1.Count == arg2.Count)) return false;    
        //  for(int i = 0; i < arg1.Count; i++)
        //          if( !wlEquals(arg1[i], arg2[i])) return false;
        //if(arg1 is string && arg2 is string)
        //  return string.op_equals(arg1,arg2);
        //return arg1 == arg2;
        var listTypeRef = _cilTypeOf(new ListTypeSymbol(TypeSymbol.Void));  // TODO: list<void> it just needs to be any list!

        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Isinst, listTypeRef);  
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);

        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Isinst, listTypeRef);
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.And);
        var lstBreak = processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        processor.Append(lstBreak);

        //if(arg1.Count != arg2.Count)
        var arg1Count = new VariableDefinition(listTypeRef);
        var arg1asList = new VariableDefinition(listTypeRef);
        var arg2asList = new VariableDefinition(listTypeRef);
        body.Variables.Add(arg1Count);
        body.Variables.Add(arg1asList);
        body.Variables.Add(arg2asList);
        
        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Castclass, listTypeRef);
        processor.Emit(OpCodes.Stloc, arg1asList);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Castclass, listTypeRef);
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

        //if(arg1.count == 0) return true;
        var ldArg1Count = processor.Create(OpCodes.Ldloc, arg1Count);
        processor.Append(ldArg1Count);
        ifCountMatch.Operand = ldArg1Count;
        var ifCountNot0 = processor.Create(OpCodes.Brtrue, processor.Create(OpCodes.Nop));
        processor.Append(ifCountNot0);
        processor.Emit(OpCodes.Ldc_I4_1);
        processor.Emit(OpCodes.Br, returnInstr);
  
        //for(int i = 0; i < arg1.Count; i++)
        var i = new VariableDefinition(_cilTypeOf(TypeSymbol.Int));
        body.Variables.Add(i);
        var newIValue = processor.Create(OpCodes.Ldc_I4_0);
        processor.Append(newIValue);
        ifCountNot0.Operand = newIValue;
        processor.Emit(OpCodes.Stloc, i);

        var breakToCond = processor.Create(OpCodes.Br, processor.Create(OpCodes.Nop));
        processor.Append(breakToCond);

        //loop body
        //if(!wlEquals(arg1[i], arg2[i])) return false;
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

        //loop condition        
        var condStart = processor.Create(OpCodes.Ldloc, i);
        processor.Append(condStart);
        breakToCond.Operand = condStart;
        processor.Emit(OpCodes.Ldloc, arg1Count);
        processor.Emit(OpCodes.Clt);                    // i < arg1.count
        processor.Emit(OpCodes.Brtrue, firstInBody);    //if (i < arg1.count) goto loopbody;
        processor.Emit(OpCodes.Ldc_I4_1);               
        processor.Emit(OpCodes.Br, returnInstr);        //made it all the way through the loop, return true

        //elseif(arg1 is string ?)
        var strStart = processor.Create(OpCodes.Ldarg, arg1);
        processor.Append(strStart);
        lstBreak.Operand = strStart;
        processor.Emit(OpCodes.Isinst, _cilTypeOf(TypeSymbol.String));
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Isinst, _cilTypeOf(TypeSymbol.String));
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Cgt_Un);
        processor.Emit(OpCodes.And);
        var strBreak = processor.Create(OpCodes.Brfalse, processor.Create(OpCodes.Nop));
        processor.Append(strBreak);
        processor.Emit(OpCodes.Ldarg, arg1);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Call, _stringEqual);
        processor.Emit(OpCodes.Br, returnInstr);
        
        //else primitive equals --uses object.equals();
        var primitiveStart = processor.Create(OpCodes.Ldarg, arg1);
        strBreak.Operand = primitiveStart;
        processor.Append(primitiveStart);
        processor.Emit(OpCodes.Ldarg, arg2);
        processor.Emit(OpCodes.Call, _objEquals);
        processor.Append(returnInstr);

        body.OptimizeMacros();

        Console.WriteLine("___WL_EQUALS START___");
        foreach(var instr in body.Instructions)
            Console.WriteLine(instr);
        Console.WriteLine("___WL_EQUALS END___\n\n\n");


        return wlEqualsDef;
    }
}