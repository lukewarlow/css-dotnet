using System.Text;

namespace CSSDotNet.Parser;

public class Token : BaseClass
{
    public Token(TokenType type)
    {
        Type = type;
    }

    public Token(TokenType type, string text)
    {
        Type = type;
        Text = text;
    }

    public Token(TokenType type, string text, string value)
    {
        Type = type;
        Text = text;
        Value = value;
    }

    public Token(TokenType type, string text, Number number)
    {
        Type = type;
        Text = text;
        Number = number;
    }
    
    public TokenType Type { get; set; }
    public Position Start { get; set; } = new();
    public Position End { get; set; } = new();
    public StringBuilder ValueBuilder { get; set; } = new(); // Unit for dimension token 
    public string Value
    {
        get => ValueBuilder.ToString();
        set => ValueBuilder = new(value);
    }
    public Number? Number { get; set; }
    public StringBuilder TextBuilder { get; set; } = new();
    public string Text
    {
        get => TextBuilder.ToString();
        init => TextBuilder = new(value);
    }

    public override string ToString()
    {
        if (Type is TokenType.String or TokenType.Unknown or TokenType.Ident or TokenType.Comment or TokenType.Hash or TokenType.AtKeyword or TokenType.Number or TokenType.Dimension or TokenType.Percentage)
        {
            return $"{Type}: {Text}";
        }
        return Type.ToString();
    }

    public TokenType? GetMirrorVariant()
    {
        return Type switch
        {
            TokenType.CurlyOpen => TokenType.CurlyClose,
            TokenType.CurlyClose => TokenType.CurlyOpen,
            TokenType.ParenOpen => TokenType.ParenClose,
            TokenType.ParenClose => TokenType.ParenOpen,
            TokenType.SquareOpen => TokenType.SquareClose,
            TokenType.SquareClose => TokenType.SquareOpen,
            _ => null
        };
    }
}

public class Position
{
    public int Line { get; set; }
    public int Column { get; set; }
}



public class Number
{
    public Number(string type, string text, double value, char? sign)
    {
        Type = type;
        Text = text;
        Value = value;
        Sign = sign;
    }
    public Number(string type, double value, char? sign)
    {
        Type = type;
        Value = value;
        Sign = sign;
    }
        
    public string Type { get; set; }
    public string? Text { get; set; }
    public double Value { get; set; }
    public char? Sign { get; set; }
}

public enum TokenType
{
    Ident,
    Function,
    AtKeyword,
    Hash,
    String,
    BadString,
    Url,
    BadUrl,
    Delim,
    Number,
    Percentage,
    Dimension,
    UnicodeRange,
    Whitespace,
    Colon,
    Semicolon,
    Comma,
    SquareOpen,
    SquareClose,
    ParenOpen,
    ParenClose,
    CurlyOpen,
    CurlyClose,
    EOF, // This is non standard but required for this implementation
    Unknown, // This is non standard
    Comment, // This is non standard
    CDO,
    CDC,
}