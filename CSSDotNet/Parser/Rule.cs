using System.Text;

namespace CSSDotNet.Parser;

public class Rule
{
    public RuleType Type { get; set; }
    public string? Name { get; set; }
    public List<ComponentValue> Prelude { get; set; } = new();
    public List<Declaration> Declarations { get; set; } = new();
    public List<Rule> ChildRules { get; set; } = new();
    // Extra internal information not required by the spec
    public List<Token> Tokens { get; set; } = new();

    public Rule(Token token)
    {
        if (token.Type is not TokenType.AtKeyword)
            throw new ArgumentException("Rule must be instantiated with an at-keyword token.", nameof(token));
        
        Type = RuleType.AtRule;
        Name = token.Value;
        Tokens.Add(token);
    }
    
    public Rule()
    {
        Type = RuleType.QualifiedRule;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(Type.ToString());
        if (Name != null)
        {
            result.Append(' ');
            result.Append(Name);
        }
        // if (Prelude.Any())
        // {
        //     result.Append('\n');
        //     result.Append("Prelude:\n");
        // }
        // result.Append(string.Join("\n", Prelude.Select(x => x.ToString())));
        // if (Declarations.Any())
        // {
        //     result.Append('\n');
        //     result.Append("Declarations:\n");
        // }
        // result.Append(string.Join("\n", Declarations.Select(x => x.ToString())));
        // if (ChildRules.Any())
        // {
        //     result.Append('\n');
        //     result.Append("ChildRules:\n");
        // }
        // result.Append(string.Join("\n", ChildRules.Select(x => x.ToString())));
        if (Tokens.Any())
        {
            result.Append('\n');
            result.Append("Tokens:\n");
        }
        result.Append(string.Join("\n", Tokens.Select(x => x.ToString())));
        return result.ToString();
    }
}

public enum RuleType
{
    AtRule,
    QualifiedRule
}