namespace WarmLangLexerParser.Read;

/// <summary>
/// Gives a window into a string, allowing one to see only parts of a string at a time. Useful for Lexer :)
/// 
/// TODO: It doesn't fully act like FileWindow, it puts EOF in different spots.
///For example "var x = 2;" ends on line 1, but FileWindow ends on line 2 ??
///Maybe it is actually FileWindow that is broken, either way, they don't agree on EOF.
/// </summary>
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
        for(; _index < Length && CurChar != '\n'; _index++) { }
        UpdateLineCounter();
        
        _index++;
    }

    public override void AdvanceText()
    {
        _index++;
        UpdateColumnCounter();
        if(!IsEndOfFile && CurChar == '\n') 
        { 
            UpdateLineCounter(); 
            _index++;
        }
    }

    public override char Peek()
    {
        return CurChar;
    }
}