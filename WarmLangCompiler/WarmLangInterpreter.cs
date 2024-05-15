namespace WarmLangCompiler;

using WarmLangCompiler.Interpreter;
using WarmLangLexerParser;
using WarmLangLexerParser.AST;
public static class WarmLangInterpreter
{
    public static Value Run(ASTNode root)
    {
        var venv = new VarEnv();
        var fenv = new FuncEnv();
        try {
            var (returned, _,_) = Evaluate(root, venv, fenv);
            return returned;
        } catch (Exception e)
        {
            return new StrValue(e.Message);
        }
    }

    public static (Value, IAssignableEnv<Value>, IEnv<Funct>) Evaluate(ASTNode node, IAssignableEnv<Value> env, IEnv<Funct> fenv)
    {
        switch(node)
        {
            case ConstExpression c: {
                return (new IntValue(c.Value), env, fenv);
            }
            case VarExpression var: {
                var value = env.Lookup(var.Name);
                return (value, env, fenv);
            }
            case BinaryExpression cur: {
                var (left, leftEnv, _) = Evaluate(cur.Left, env, fenv);
                var (right, resEnv, _) = Evaluate(cur.Right, leftEnv, fenv);

                var op = cur.Operation;
                var res = (op,left,right) switch 
                {
                    ("+", IntValue i1, IntValue i2) => new IntValue(i1.Value + i2.Value),
                    ("*", IntValue i1, IntValue i2) => new IntValue(i1.Value * i2.Value),
                    ("-", IntValue i1, IntValue i2) => new IntValue(i1.Value - i2.Value),
                    //TODO: When we introduce booleans, change ones below
                    ("==", IntValue i1, IntValue i2) => new IntValue(i1.Value == i2.Value ? 1 : 0), 
                    ("<", IntValue i1, IntValue i2) => new IntValue(i1.Value < i2.Value ? 1 : 0), 
                    ("<=", IntValue i1, IntValue i2) =>  new IntValue(i1.Value <= i2.Value ? 1 : 0),
                    _ => throw new NotImplementedException($"Failed: Operator: \"{op}\" on {left.GetType().Name} and {right.GetType().Name} is not defined")
                };

                return (res, resEnv, fenv);
            }
            case UnaryExpression unary: {
                var (exprValue, newVarEnv, newFuncEnv) = Evaluate(unary.Expression, env, fenv);
                var value = (unary.Operation, exprValue) switch 
                {
                    ("+", IntValue i) => i,  //do nothing for the (+1) cases
                    ("-", IntValue i) => new IntValue(-i.Value), //flip it for the (-1) cases
                    _ => throw new NotImplementedException($"Failed: Unary {unary.Operation} is not defined on {exprValue.GetType()}")
                };
                return (value, newVarEnv, newFuncEnv);
            }
            case VarDeclarationExpression decl: {
                var name = decl.Name;
                var (value, eEnv,_) = Evaluate(decl.RightHandSide, env, fenv);
                var (_, nextEnv) = eEnv.Declare( name, value);
                return (value, (VarEnv)nextEnv, fenv);
            }
            case VarAssignmentExpression assignment: {
                var name = assignment.Name;
                var (value, eEnv,_) = Evaluate(assignment.RightHandSide, env, fenv);
                var (res, nEnv) = eEnv.Assign(name, value);
                return (res, (VarEnv)nEnv, fenv);
                //Use eEnv because in the future we may want to allow something like
                // var x = 10; var y = 5; x = y++;
                // which would update both x and y.
            }
            case CallExpression call: {
                var name = call.Name;
                var callArgs = call.Arguments;
                var (functionParameters, funcBody) = fenv.Lookup(name);
                
                var callVarScope = env.Push();
                var callFunScope = fenv.Push();

                foreach(var ((paramType, paramName), expr) in functionParameters.Zip(callArgs)) 
                {
                    var (value, nEnv, nFEnv) = Evaluate(expr, (VarEnv)callVarScope, callFunScope);

                    //This is an attempt at prevention a function being called with arguments of wrong types.
                    //something like : "function func(int x) { x + 2; } 
                    //                  func(true)"  <-- true is not an int?!?!
                    var (_, nVarEnv) = (value, paramType) switch 
                    {
                        (IntValue _, TokenKind.TInt) => nEnv.Declare(paramName, value),
                        _ => throw new Exception($"Value of {value.GetType().Name} does not match function paramter type {paramType}")
                    };
                    
                    callVarScope = nVarEnv;
                    callFunScope = nFEnv; //not too sure about this one :)
                }
                var (returnedValue, retVarEnv, retFuncEnv) = Evaluate(funcBody,(VarEnv)callVarScope, callFunScope);
                return (returnedValue, (VarEnv)retVarEnv.Pop(), retFuncEnv.Pop());
            }
            case FuncDeclaration funDecl: {
                var funcName = funDecl.Name;
                var paramNames = funDecl.Params;
                var body = funDecl.Body;
                var (function, newFEnv) = fenv.Declare(funcName, new Funct(paramNames, body));
                return (new IntValue(0), env, newFEnv);
            }
            case ExprStatement expr: {
                return Evaluate(expr.Expression, env, fenv);
            }
            case BlockStatement block: {
                var expressions = block.Children;
                var nenv = env.Push();
                var nFuncEnv = fenv.Push();
                for (int i = 0; i < expressions.Count-1; i++)
                {
                    var expr = expressions[i];
                    var (_, nenv2, fenv2) = Evaluate(expr, (VarEnv)nenv, fenv);
                    nenv = nenv2;
                    nFuncEnv = fenv2;
                }
                var last = expressions[^1];
                
                var (lastValue, lastEnv, lastFuncEnv) = Evaluate(last, (VarEnv)nenv, nFuncEnv);
                return (lastValue, (VarEnv)lastEnv.Pop(), lastFuncEnv.Pop()); //Return an unaltered environment - to throw away any variables declared in block.
            }
            case IfStatement ifstmnt: {
                var ifScope = env.Push();
                var ifFuncScope = fenv.Push();
                var (condValue, cEnv, cFuncEnv) = Evaluate(ifstmnt.Condition, (VarEnv) ifScope, ifFuncScope);
                
                bool doThenBranch = condValue switch //TODO: When we introduce booleans, look at this again :)
                {
                    IntValue i => i.Value != 0,
                    _ => throw new NotImplementedException($"Failed: value of {condValue.GetType()} cannot be used as boolean")
                };

                if(!doThenBranch && ifstmnt.Else is null)
                {
                    //What to return in this case?
                    return (new IntValue(0), (VarEnv)cEnv.Pop(), cFuncEnv.Pop());
                }
                var (value, nEnv, nFuncEnv) = Evaluate(doThenBranch ? ifstmnt.Then : ifstmnt.Else!, cEnv, fenv);
                return (value, (VarEnv)nEnv.Pop(), nFuncEnv.Pop());
            }
            default: {
                throw new NotImplementedException($"Unsupported Expression: {node.GetType()}");
            }
        }
    }
}