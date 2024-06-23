namespace WarmLangLexerParser.Read;

/// <summary>
/// FileWindow gives us a "window" into some file, that is we can see parts of the file at a time :)
/// </summary>
public sealed class FileWindow : TextWindow
{
    private readonly StreamReader _reader;
    
    private char _cur;

    private void Read()
    {
        if(_reader.Peek() >= 0)
        {
            _cur = (char) _reader.Read();
        }
        else
        {
            _cur = '\0';
        }
    }

    private FileWindow(StreamReader reader) : base()
    {
        _reader = reader;
        Read();
    }

    public FileWindow(IFileReader reader) : this(reader.GetStreamReader()) { }

    public FileWindow(string filePath) : this(new StreamReader(filePath)) { }

    public override bool IsEndOfFile => _reader.EndOfStream && _cur == '\0';

    public override void AdvanceLine()
    {
        while(_cur != '\n' && !IsEndOfFile)
        {
            Read();
            UpdateColumnCounter();
        }
        Read(); //Consume the '\n'
        if(!IsEndOfFile)
            UpdateLineCounter();
    }

    public override void AdvanceText()
    {
        UpdateColumnCounter();
        if(_cur == '\n')
        {
            UpdateLineCounter();
        }
        Read();
    }

    public override char Peek()
    {
        if(!IsEndOfFile)
            return _cur;
        Console.WriteLine((string?)$"LEXER Failed: on line: {Line+1}, column: {Column+1}");
        return '\0';
    }

    private void DebugPrint(string prefix = "")
    {
        Console.WriteLine($"{prefix} cur:'{_cur}'");
    }
}