namespace WarmLangCompiler.ILGen;

using System;
using WarmLangCompiler.Binding;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser;

public sealed class Emitter : IDisposable
{

    private static readonly Dictionary<TypeSymbol, string> TypeSymbolToILType = new()
    {
        {TypeSymbol.Int,"int32"}, {TypeSymbol.Bool, "bool"},
        {TypeSymbol.String,"System.String"}, //TODO: lists?
    };

    //private readonly List<byte> _instructions;
    private readonly StreamWriter _writer;

    private void Emitstring(string o)
    {
        Console.WriteLine($"Writing '{o}'");
        _writer.WriteLine(o);
    }

    public void Dispose() => _writer.Dispose();

    public Emitter()
    {
        _writer = new(Path.Combine(Directory.GetCurrentDirectory(), "testproj/out.il"), false);
    }

    public void EmitProgram(BoundProgram program)
    {
        _writer.WriteLine(".assembly extern mscorlib {}");
        _writer.WriteLine(".assembly 'App' {}");
        _writer.WriteLine(".class private auto ansi beforefieldinit abstract sealed Program extends [mscorlib]System.Object {");
        _writer.WriteLine(".method private hidebysig static void Main ( string[] args) cil managed {");
        _writer.WriteLine(".entrypoint");
        EmitStatement(program.Statement);
        _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(int32)");
        _writer.WriteLine("ret } }");
    }

    private void EmitStatement(BoundStatement statement)
    {
        switch(statement)
        {
            case BoundBlockStatement block:
                EmitBlockStatement(block);
                break;
            case BoundExprStatement expr:
                EmitExpression(expr.Expression);
                break;
        }
    }

    private void EmitBlockStatement(BoundBlockStatement block)
    {
        foreach(var statement in block.Statements)
            EmitStatement(statement);
    }

    private void EmitExpression(BoundExpression expr)
    {
        switch(expr)
        {
            case BoundBinaryExpression binary:
                EmitBinaryExpression(binary);
                break;
            case BoundConstantExpression bound:
                EmitConstantExpression(bound);
                break;
        }
    }


    private void EmitBinaryExpression(BoundBinaryExpression binary)
    {
        EmitExpression(binary.Left);
        EmitExpression(binary.Right);
        Emitstring("  " + binary.Operator.Kind.ILInstruction());
    }

    private void EmitConstantExpression(BoundConstantExpression bound)
    {
        if(bound.Type == TypeSymbol.String)
        {
            Emitstring($"  ldstr \"{bound.Constant.GetCastValue<string>()}\"");
        }
        if(bound.Type == TypeSymbol.Bool)
        {
            if(bound.Constant.GetCastValue<bool>())
                Emitstring($"  ldc.i4.0");
            else 
                Emitstring($"  ldc.i4.0");
        }
        if(bound.Type == TypeSymbol.Int)
        {
            Emitstring($"  ldc.i4.s {bound.Constant.GetCastValue<int>()}");
        }   
    }
} 