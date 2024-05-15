namespace WarmLangLexerParser.Read;

public abstract class TextWindow
{
    public int Column { get; protected set; }
    public int Line { get; protected set; }

    public abstract bool IsEndOfFile { get; }

    public abstract void AdvanceText();
    public abstract void AdvanceLine();

    public abstract char Peek();

    public TextWindow()
    {
        Column = Line = 0;
    }
    
    /// <summary>
    /// Performs the operations on Line and Column expected when starting to read a new line.
    /// </summary>
    protected void UpdateLineCounter()
    {
        Column = 0;
        Line++;
    }

    /// <summary>
    /// Performs the operations on Column expected when going a character forward.
    /// </summary>
    protected void UpdateColumnCounter() => Column++;
    
}