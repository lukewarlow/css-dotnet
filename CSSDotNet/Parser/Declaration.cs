using System.Text;

namespace CSSDotNet.Parser;

public class Declaration
{
    public string Name { get; set; }
    public List<ComponentValue> Value { get; set; } = new();
    public bool Important { get; set; }
    public string? OriginalText { get; set; }
    // Extra internal information not required by the spec
    public List<Token> Tokens { get; set; } = new();
    public bool Bad { get; set; }
    
    public Declaration(Token token)
    {
        if (token.Type is not TokenType.Ident)
            throw new ArgumentException("Declaration must be instantiated with an ident token.", nameof(token));
        Name = token.Value;
        Tokens.Add(token);
    }
    
    

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append($"Declaration: {Name} Important: {Important}");
        if (Value.Any())
        {
            result.Append('\n');
            result.Append("Value:\n");
        }
        result.Append(string.Join("\n", Value.Select(x => x.ToString())));
        if (Tokens.Any())
        {
            result.Append('\n');
            result.Append("Tokens:\n");
        }
        result.Append(string.Join("\n", Tokens.Select(x => x.ToString())));
        return result.ToString();
    }
}