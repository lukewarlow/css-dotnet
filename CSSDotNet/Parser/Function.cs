namespace CSSDotNet.Parser;

public class Function : BaseClass
{
    public string Name { get; set; }
    public List<ComponentValue> Value { get; set; } = new();
    // Extra internal information not required by the spec
    public List<Token> Tokens { get; set; } = new();
    
    public Function(Token token)
    {
        if (token.Type is not TokenType.Function)
            throw new ArgumentException("Function must be instantiated with a function token.", nameof(token));
        Name = token.Value;
        Tokens.Add(token);
    }
}