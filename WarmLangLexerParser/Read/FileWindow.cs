namespace WarmLangLexerParser.Read;

/// <summary>
/// FileWindow gives us a "window" into some file, that is we can see parts of the file at a time :)
/// </summary>
public sealed class FileWindow : TextWindow
{
    private readonly StreamReader _reader;
    private string _curLine;

    private FileWindow(StreamReader reader) : base()
    {
        _reader = reader;
        _curLine = _reader.ReadLine() ?? "";
    }

    public FileWindow(IFileReader reader) : this(reader.GetStreamReader()) { }

    public FileWindow(string filePath) : this(new StreamReader(filePath)) { }

    public override bool IsEndOfFile => _reader.EndOfStream && _curLine == "";

    public override void AdvanceLine()
    {
        string? line = null;
        while(!_reader.EndOfStream && string.IsNullOrWhiteSpace(line = _reader.ReadLine())) 
        {
            Line++;
        }
        _curLine = line ?? "";
        UpdateLineCounter();
    }

    public override void AdvanceText()
    {
        UpdateColumnCounter();
        if(Column >= _curLine.Length)
        {
            string? line = "";
            while(!_reader.EndOfStream && string.IsNullOrWhiteSpace(line = _reader.ReadLine())) 
            {
                Line++;
            }
            _curLine = line ?? "";
            UpdateLineCounter();
        }
    }

    public override char Peek()
    {
        try 
        {
            return _curLine[Column];
        } catch (Exception)
        {
            Console.WriteLine($"LEXER Failed: on line: {Line+1}, column: {Column+1}");
            throw;
        }
    }
}