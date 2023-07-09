using System.Text;

namespace CSSDotNet.Parser;

public class ComponentValue
{
    // TODO potentially just have 3 properties for each type of value
    public BaseClass? Value { get; set; }
    
    // Extra internal information not required by the spec
    public List<Token> Tokens { get; set; } = new();
    
    public ComponentValue(Token token)
    {
        Tokens.Add(token);
        Value = token;
    }
    
    public ComponentValue(Block block)
    {
        block.Tokens.ForEach(token => Tokens.Add(token));
        Value = block;
    }
    
    public ComponentValue(Function function)
    {
        function.Tokens.ForEach(token => Tokens.Add(token));
        Value = function;
    }
    
    public bool IsToken => Value is Token;
    public Token AsToken => Value as Token ?? throw new InvalidCastException();
    
    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append($"ComponentValue\n");
        result.Append(Value);
        if (Tokens.Any())
        {
            result.Append('\n');
            result.Append("Tokens:\n");
        }
        result.Append(string.Join("\n", Tokens.Select(x => x.ToString())));
        return result.ToString();
    }
}