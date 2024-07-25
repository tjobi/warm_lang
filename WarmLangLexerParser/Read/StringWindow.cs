namespace WarmLangLexerParser.Read;

public sealed class StringWindow : TextWindow
{
    
    private readonly string _input;
    private int _index;

    public StringWindow(string str) : base()
    {
        _input = str;
    }

    public override bool IsEndOfFile => _index >= Length;
    private int Length => _input.Length;
    private char CurChar => _input[_index];
    
    public override void AdvanceLine()
    {
        for(; _index < Length && CurChar != '\n'; _index++)
        {
            UpdateColumnCounter();
        }
        _index++;

        if(!IsEndOfFile)
            UpdateLineCounter();
    }

    public override void AdvanceText()
    {
        UpdateColumnCounter();
        if(!IsEndOfFile && CurChar == '\n') 
        { 
            UpdateLineCounter(); 
        }
        _index++;
    }

    public override char Peek()
    {
        return CurChar;
    }
}