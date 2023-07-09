using System.Text;

namespace CSSDotNet.Parser;

public class Stylesheet
{
    public Uri? Location { get; set; }
    public List<Rule> Value { get; set; } = new();
    
    public Stylesheet(Uri? location = null)
    {
        Location = location;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append("Stylesheet ");
        if (Location != null)
        {
            result.Append('(');
            result.Append(Location);
            result.Append(')');
        }

        result.Append('\n');
        result.Append(string.Join("\n", Value.Select(rule => rule.ToString())));
        return result.ToString();
    }
}