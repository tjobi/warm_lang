using WarmLangLexerParser.AST;

namespace WarmLangCompiler.Binding.Lower;

public class BoundLabel
{
    public BoundLabel(int labelNum)
    {
        Name = $"L{labelNum}";
    }

    public string Name { get; }

    public override string ToString() => Name;
}