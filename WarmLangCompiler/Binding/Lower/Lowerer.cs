using System.Collections.Immutable;
using WarmLangCompiler.Symbols;
using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public static class Lowerer
{   
    private static int _nextLabelNum;

    private static BoundLabel GenerateLabel() => new(_nextLabelNum++);

    private static BoundBlockStatement Block(BoundStatement syntax, params BoundStatement[] statements)
    => new(syntax.Node, statements.ToImmutableArray());

    public static BoundBlockStatement LowerProgram(BoundBlockStatement blockStatement) => Flatten(null, RewriteBlockStatement(blockStatement));

    public static BoundBlockStatement LowerBody(FunctionSymbol function, BoundBlockStatement blockStatement)
    {
        return Flatten(function, RewriteBlockStatement(blockStatement));
    }

    private static BoundBlockStatement Flatten(FunctionSymbol? function, BoundBlockStatement statement)
    {
        var flatStatements = ImmutableArray.CreateBuilder<BoundStatement>();
        var stack = new Stack<BoundStatement>();

        foreach(var stmnt in statement.Statements)
        {
            stack.Push(stmnt);
            while(stack.Count != 0)
            {
                var current = stack.Pop();
                if(current is BoundBlockStatement bs)
                {
                    for (int i = bs.Statements.Length - 1; i >= 0 ; i--)
                    {
                        stack.Push(bs.Statements[i]);
                    }
                } else 
                {
                    flatStatements.Add(current);
                }
            }
        }
        if(function is not null)
        {
            var last = flatStatements.LastOrDefault();
            if(function.Type == TypeSymbol.Void && 
               last is null or not BoundReturnStatement)
            {
                flatStatements.Add(new BoundReturnStatement(statement.Node));
            }
        }

        return new BoundBlockStatement(statement.Node, flatStatements.ToImmutable());
    }

    public static BoundStatement RewriteStatement(BoundStatement statement)
    {
        return statement switch
        {
            BoundWhileStatement wile => RewriteWhileStatement(wile),
            BoundIfStatement iff when iff.Else is not null => RewriteIfElseStatement(iff),
            BoundIfStatement iff when iff.Else is null => RewriteIfStatement(iff),
            BoundBlockStatement block => RewriteBlockStatement(block),
            BoundFunctionDeclaration func when func.Symbol is LocalFunctionSymbol symbol => RewriteFunctionDeclaration(func, symbol),
            _ => statement,
        };
    }

    private static BoundStatement RewriteFunctionDeclaration(BoundFunctionDeclaration func, LocalFunctionSymbol symbol)
    {
        if(symbol.Body is null)
            return func;
        symbol.Body = LowerBody(symbol, symbol.Body);
        return func;
    }

    private static BoundBlockStatement RewriteBlockStatement(BoundBlockStatement block)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        foreach(var stmnt in block.Statements)
        {
            var rwrten = RewriteStatement(stmnt); 
            statements.Add(rwrten);
        }
        var rewritten = new BoundBlockStatement(block.Node, statements.ToImmutable());
        return rewritten;
    }

    private static BoundStatement RewriteWhileStatement(BoundWhileStatement wile)
    {
        //   goto <labelCondition>
        // labelWhilebody:
        //   <whileBody>
        //   <whileContinuation>
        // labelCondition:
        //   if (condition) goto <labelWhileBody>; else goto <labelWhileEnd>;
        // labelWhileEnd:
        // ..
        var labelCondition = GenerateLabel();
        var labelWhileBody = GenerateLabel();
        var labelWhileEnd = GenerateLabel();
        var builder = ImmutableArray.CreateBuilder<BoundStatement>();
        builder.Add(new BoundGotoStatement(wile.Node, labelCondition));
        builder.Add(new BoundLabelStatement(wile.Node, labelWhileBody));
        builder.Add(wile.Body);
        foreach(var cont in wile.Continue)
        {
            builder.Add(new BoundExprStatement(new ExprStatement(cont.Node), cont));
        }
        builder.Add(new BoundLabelStatement(wile.Node, labelCondition));
        builder.Add(new BoundConditionalGotoStatement(wile.Node, wile.Condition, labelWhileBody, labelWhileEnd));
        builder.Add(new BoundLabelStatement(wile.Node, labelWhileEnd));
        return RewriteStatement(new BoundBlockStatement(wile.Node, builder.ToImmutable()));
    }

    private static BoundStatement RewriteIfStatement(BoundIfStatement iff)
    {
        //   if (condition) goto <labelTrue>; else goto <labelEnd>
        // labelThen
        //   *code for thenBranch*
        // labelEnd:
        var labelThen = GenerateLabel();
        var labelEnd = GenerateLabel();
        var result = Block(iff,
            new BoundConditionalGotoStatement(iff.Node, iff.Condition, labelThen, labelEnd),
            new BoundLabelStatement(iff.Node, labelThen),
            iff.Then,
            new BoundLabelStatement(iff.Node, labelEnd)
        );
        return RewriteStatement(result);
    }

    private static BoundStatement RewriteIfElseStatement(BoundIfStatement iff)
    {
        //   if (condition) goto labelThen; else goto <labelElse>;
        // labelThen:
        //   *code for thenBranch*
        //   goto <labelIfElseEnd>
        // labelElse:
        //   <elseBranch>
        // labelIfElseEnd:
        var labelThen = GenerateLabel();
        var labelElse = GenerateLabel();
        var labelEnd = GenerateLabel();
        var result = Block(iff,
            new BoundConditionalGotoStatement(iff.Node, iff.Condition, labelThen, labelElse),
            new BoundLabelStatement(iff.Node, labelThen),
            iff.Then,
            new BoundGotoStatement(iff.Node, labelEnd),
            new BoundLabelStatement(iff.Node, labelElse),
            iff.Else!,
            new BoundLabelStatement(iff.Node, labelEnd)
        );
        return RewriteStatement(result);
    }
}