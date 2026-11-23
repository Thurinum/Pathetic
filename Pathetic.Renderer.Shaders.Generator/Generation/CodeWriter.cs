using System.Text;

namespace Pathetic.Renderer.Shaders.Generator.Generation;

internal sealed class CodeWriter
{
    // Generated files are checked in as CRLF text, regardless of the build host.
    private const string NewLine = "\r\n";

    private readonly StringBuilder _builder = new();
    private int _indent;

    public void Line(string text = "")
    {
        if (text.Length > 0)
            _builder.Append(' ', _indent * 4).Append(text);

        _builder.Append(NewLine);
    }

    public void Open(string text)
    {
        Line(text);
        _indent++;
    }

    public void Close(string text)
    {
        _indent--;
        Line(text);
    }

    public override string ToString() => _builder.ToString();
}
