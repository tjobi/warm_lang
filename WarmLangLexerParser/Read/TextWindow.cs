namespace WarmLangLexerParser.Read;

public abstract class TextWindow
{
    public int Column { get; protected set; }
    public int Line { get; protected set; }

    public char Current { get; protected set;}
    public abstract bool IsEndOfFile { get; }

    public abstract void AdvanceText();
    public abstract void AdvanceLine();

    public abstract char Peek();
    
}