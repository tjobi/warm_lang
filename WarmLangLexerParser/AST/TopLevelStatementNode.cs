namespace WarmLangLexerParser.AST;


public abstract class TopLevelNode : ASTNode
{
    protected TopLevelNode(TextLocation location) : base(location) { }
}

/// <summary>
/// Any statement that exists outside of a function body
/// The idea is to allow either TopLevelStatments or a main function.
///     Any TopLevelStatements will be collected into an implicit main function
/// </summary>
public abstract class TopLevelStamentNode : TopLevelNode
{
    public TopLevelStamentNode(StatementNode stmnt) : base(stmnt.Location) {}

    public abstract StatementNode Statement { get; }
}