namespace CSSDotNet.Parser;

public class Block : BaseClass
{
    public Token Token { get; set; }
    public List<ComponentValue> Value { get; set; } = new();
    // Extra internal information not required by the spec
    public List<Token> Tokens { get; set; } = new();
    
    public Block(Token token)
    {
        Token = token;
        Tokens.Add(token);
    }
}