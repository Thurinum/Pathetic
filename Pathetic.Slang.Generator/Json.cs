using System.Globalization;
using System.Text;

namespace Pathetic.Slang.Generator;

internal abstract class JsonValue
{
}

internal sealed class JsonObject : JsonValue
{
    public Dictionary<string, JsonValue> Members { get; } = new(StringComparer.Ordinal);

    public JsonValue? this[string name] => Members.TryGetValue(name, out JsonValue? value) ? value : null;
}

internal sealed class JsonArray : JsonValue
{
    public List<JsonValue> Items { get; } = [];
}

internal sealed class JsonString : JsonValue
{
    public string Value { get; set; } = "";
}

internal sealed class JsonNumber : JsonValue
{
    public long Value { get; set; }
}

internal sealed class JsonBool : JsonValue
{
    public bool Value { get; set; }
}

internal sealed class JsonNull : JsonValue
{
    public static JsonNull Instance { get; } = new();
}

internal sealed class JsonReader
{
    private readonly string _text;
    private int _index;

    private JsonReader(string text)
    {
        _text = text;
    }

    public static JsonValue Parse(string text)
    {
        JsonReader reader = new(text);
        JsonValue value = reader.ReadValue();
        reader.SkipWhitespace();

        if (reader._index != text.Length)
            throw reader.Error($"Unexpected trailing character '{text[reader._index]}'.");

        return value;
    }

    private JsonValue ReadValue()
    {
        SkipWhitespace();

        if (_index >= _text.Length)
            throw Error("Unexpected end of JSON.");

        char current = _text[_index];

        return current switch
        {
            '{' => ReadObject(),
            '[' => ReadArray(),
            '"' => new JsonString { Value = ReadString() },
            't' => ReadLiteral("true", new JsonBool { Value = true }),
            'f' => ReadLiteral("false", new JsonBool { Value = false }),
            'n' => ReadLiteral("null", JsonNull.Instance),
            _ => ReadNumber(),
        };
    }

    private JsonObject ReadObject()
    {
        JsonObject result = new();
        _index++;
        SkipWhitespace();

        if (TryConsume('}'))
            return result;

        while (true)
        {
            SkipWhitespace();

            if (_index >= _text.Length || _text[_index] != '"')
                throw Error("Expected a property name.");

            string name = ReadString();
            SkipWhitespace();
            Expect(':');
            result.Members[name] = ReadValue();
            SkipWhitespace();

            if (TryConsume('}'))
                return result;

            Expect(',');
        }
    }

    private JsonArray ReadArray()
    {
        JsonArray result = new();
        _index++;
        SkipWhitespace();

        if (TryConsume(']'))
            return result;

        while (true)
        {
            result.Items.Add(ReadValue());
            SkipWhitespace();

            if (TryConsume(']'))
                return result;

            Expect(',');
        }
    }

    private string ReadString()
    {
        Expect('"');
        StringBuilder builder = new();

        while (true)
        {
            if (_index >= _text.Length)
                throw Error("Unterminated string.");

            char current = _text[_index++];

            if (current == '"')
                return builder.ToString();

            if (current != '\\')
            {
                builder.Append(current);
                continue;
            }

            if (_index >= _text.Length)
                throw Error("Unterminated escape sequence.");

            char escape = _text[_index++];

            switch (escape)
            {
                case '"': builder.Append('"'); break;
                case '\\': builder.Append('\\'); break;
                case '/': builder.Append('/'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'u':
                    if (_index + 4 > _text.Length)
                        throw Error("Incomplete unicode escape.");

                    builder.Append((char)int.Parse(_text.Substring(_index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    _index += 4;
                    break;
                default:
                    throw Error($"Unknown escape sequence '\\{escape}'.");
            }
        }
    }

    private JsonNumber ReadNumber()
    {
        int start = _index;

        while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] is '-' or '+' or '.' or 'e' or 'E'))
            _index++;

        string raw = _text.Substring(start, _index - start);

        if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            throw Error($"Invalid number '{raw}'.");

        return new JsonNumber { Value = value };
    }

    private JsonValue ReadLiteral(string literal, JsonValue value)
    {
        if (_index + literal.Length > _text.Length || string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
            throw Error($"Expected '{literal}'.");

        _index += literal.Length;
        return value;
    }

    private void SkipWhitespace()
    {
        while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            _index++;
    }

    private bool TryConsume(char expected)
    {
        if (_index < _text.Length && _text[_index] == expected)
        {
            _index++;
            return true;
        }

        return false;
    }

    private void Expect(char expected)
    {
        if (!TryConsume(expected))
            throw Error($"Expected '{expected}'.");
    }

    private FormatException Error(string message) => new($"{message} (at offset {_index})");
}
