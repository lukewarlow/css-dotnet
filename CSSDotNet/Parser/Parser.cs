using System.Data;
using System.Text;
using CSSDotNet.Utils;
using InvalidOperationException = System.InvalidOperationException;

namespace CSSDotNet.Parser;

// TODO ensure ALL original tokens are preserved in their higher level CSS objects
public class Parser : IDisposable, IAsyncDisposable
{
    private Tokenizer? Tokenizer { get; set; }
    
    // https://drafts.csswg.org/css-syntax/#parser-entry-points
    #region Parser Entry Points
    
    #region Normalize

    /// <summary>
    /// 1. If input is already a token stream return it.
    /// https://drafts.csswg.org/css-syntax/#normalize-into-a-token-stream
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private TokenStream Normalize(TokenStream input)
    {
        input.Init();
        return input;
    }

    /// <summary>
    /// 2a. If input is a list of CSS tokens, create a new token stream with input as its tokens and return it.
    /// https://drafts.csswg.org/css-syntax/#normalize-into-a-token-stream
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private TokenStream Normalize(IEnumerable<Token> input)
    {
        var tokenStream = new TokenStream(input);
        tokenStream.Init();
        return tokenStream;
    }

    /// <summary>
    /// 2b. If input is a list of component values, create a new token stream with input as its tokens and return it.
    /// https://drafts.csswg.org/css-syntax/#normalize-into-a-token-stream
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private TokenStream Normalize(IEnumerable<ComponentValue> input)
    {
        // TODO this will probably never be used but the spec is slightly ambiguous on what to do with non-token component values
        var tokenStream = new TokenStream(input.Where(i => i.IsToken).Select(i => i.AsToken));
        tokenStream.Init();
        return tokenStream;
    }

    /// <summary>
    /// 3. If input is a string, then filter code points from input, tokenize the result, then create a new token stream with those tokens as its tokens, and return it.
    /// https://drafts.csswg.org/css-syntax/#normalize-into-a-token-stream
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private TokenStream Normalize(string input, CancellationToken cancellationToken = default)
    {
        Tokenizer = new(input);
        var tokenStream = new TokenStream(Tokenizer.Tokenize(cancellationToken));
        tokenStream.Init();
        return tokenStream;
    }
    #endregion

    /// <summary>
    /// "Parse a stylesheet" is intended to be the normal parser entry point, for parsing stylesheets.
    /// https://drafts.csswg.org/css-syntax/#parse-stylesheet
    /// </summary>
    /// <param name="input"></param>
    /// <param name="uri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Stylesheet ParseStylesheet(Stream input, Uri? uri = null, CancellationToken cancellationToken = default)
    {
        var tokenizer = new Tokenizer(input);
        using var tokenStream = new TokenStream(tokenizer.Tokenize(cancellationToken));
        return ParseStylesheet(tokenStream, uri, cancellationToken);
    }

    /// <summary>
    /// "Parse a stylesheet" is intended to be the normal parser entry point, for parsing stylesheets.
    /// https://drafts.csswg.org/css-syntax/#parse-stylesheet
    /// </summary>
    /// <param name="input"></param>
    /// <param name="uri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Stylesheet ParseStylesheet(string input, Uri? uri = null, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseStylesheet(tokenStream, uri, cancellationToken);
    }

    /// <summary>
    /// "Parse a stylesheet" is intended to be the normal parser entry point, for parsing stylesheets.
    /// https://drafts.csswg.org/css-syntax/#parse-stylesheet
    /// </summary>
    /// <param name="input"></param>
    /// <param name="uri"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Stylesheet ParseStylesheet(TokenStream input, Uri? uri = null, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        return new(uri)
        {
            Value = ConsumeStylesheetContent(tokenStream, cancellationToken)
        };
    }

    /// <summary>
    /// "Parse a stylesheet’s contents" is intended for use by the CSSStyleSheet replace() method, and similar, which parse text into the contents of an existing stylesheet.
    /// https://drafts.csswg.org/css-syntax/#parse-stylesheet-contents
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<Rule> ParseStylesheetContent(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseStylesheetContent(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a stylesheet’s contents" is intended for use by the CSSStyleSheet replace() method, and similar, which parse text into the contents of an existing stylesheet.
    /// https://drafts.csswg.org/css-syntax/#parse-stylesheet-contents
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<Rule> ParseStylesheetContent(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        return ConsumeStylesheetContent(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a block’s contents" is intended for parsing the contents of any block in CSS (including things like the style attribute), and APIs such as the CSSStyleDeclaration cssText attribute.
    /// https://drafts.csswg.org/css-syntax/#parse-block-contents
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public (List<Declaration>, List<Rule>) ParseBlockContent(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseBlockContent(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a block’s contents" is intended for parsing the contents of any block in CSS (including things like the style attribute), and APIs such as the CSSStyleDeclaration cssText attribute.
    /// https://drafts.csswg.org/css-syntax/#parse-block-contents
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public (List<Declaration>, List<Rule>) ParseBlockContent(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        return ConsumeBlockContent(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a rule" is intended for use by the CSSStyleSheet insertRule() method, and similar, which parse text into a single rule. CSSStyleSheet#insertRule method, and similar functions which might exist, which parse text into a single rule.
    /// https://drafts.csswg.org/css-syntax/#parse-rule
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public Rule? ParseRule(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseRule(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a rule" is intended for use by the CSSStyleSheet insertRule() method, and similar, which parse text into a single rule. CSSStyleSheet#insertRule method, and similar functions which might exist, which parse text into a single rule.
    /// https://drafts.csswg.org/css-syntax/#parse-rule
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public Rule? ParseRule(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);

        DiscardWhitespace(tokenStream, cancellationToken);

        var nextToken = tokenStream.Peek();
        var rule = nextToken.Type switch
        {
            TokenType.EOF => throw new SyntaxErrorException("Unexpected end of input"),
            TokenType.AtKeyword => ConsumeAtRule(tokenStream, cancellationToken: cancellationToken),
            _ => ConsumeQualifiedRule(tokenStream, cancellationToken: cancellationToken) ?? throw new SyntaxErrorException()
        };

        DiscardWhitespace(tokenStream, cancellationToken);
        
        if (tokenStream.Peek().Type is TokenType.EOF) return rule;
        throw new SyntaxErrorException("Unexpected token");
    }

    /// <summary>
    /// "Parse a declaration" is used in @supports conditions.
    /// https://drafts.csswg.org/css-syntax/#parse-declaration
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public Declaration ParseDeclaration(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseDeclaration(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a declaration" is used in @supports conditions.
    /// https://drafts.csswg.org/css-syntax/#parse-declaration
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public Declaration ParseDeclaration(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        DiscardWhitespace(tokenStream, cancellationToken);
        return ConsumeDeclaration(tokenStream, cancellationToken: cancellationToken) ?? throw new SyntaxErrorException();
    }

    /// <summary>
    /// "Parse a component value" is for things that need to consume a single value, like the parsing rules for attr().
    /// https://drafts.csswg.org/css-syntax/#parse-component-value
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public ComponentValue ParseComponentValue(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseComponentValue(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a component value" is for things that need to consume a single value, like the parsing rules for attr().
    /// https://drafts.csswg.org/css-syntax/#parse-component-value
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SyntaxErrorException"></exception>
    public ComponentValue ParseComponentValue(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        DiscardWhitespace(tokenStream, cancellationToken);
        if (tokenStream.Peek().Type is TokenType.EOF) throw new SyntaxErrorException();
        var value = ConsumeComponentValue(tokenStream, cancellationToken);
        DiscardWhitespace(tokenStream, cancellationToken);
        if (tokenStream.Peek().Type is TokenType.EOF) return value;
        throw new SyntaxErrorException();
    }

    /// <summary>
    /// "Parse a list of component values" is for the contents of presentational attributes, which parse text into a single declaration’s value, or for parsing a stand-alone selector or list of Media Queries, as in Selectors API or the media HTML attribute.
    /// https://drafts.csswg.org/css-syntax/#parse-list-of-component-values
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<ComponentValue> ParseComponentValues(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseComponentValues(tokenStream, cancellationToken);
    }

    /// <summary>
    /// "Parse a list of component values" is for the contents of presentational attributes, which parse text into a single declaration’s value, or for parsing a stand-alone selector or list of Media Queries, as in Selectors API or the media HTML attribute.
    /// https://drafts.csswg.org/css-syntax/#parse-list-of-component-values
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<ComponentValue> ParseComponentValues(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        return ConsumeComponentValues(tokenStream, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#parse-comma-separated-list-of-component-values
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<List<ComponentValue>> ParseComponentValuesCSV(string input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input, cancellationToken);
        return ParseComponentValuesCSV(tokenStream, cancellationToken);
    }
    
    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#parse-comma-separated-list-of-component-values
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<List<ComponentValue>> ParseComponentValuesCSV(TokenStream input, CancellationToken cancellationToken = default)
    {
        using var tokenStream = Normalize(input);
        var groups = new List<List<ComponentValue>>();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextToken = tokenStream.Peek();
            if (nextToken.Type is TokenType.EOF) break;
            var group = ConsumeComponentValues(tokenStream, TokenType.Comma, cancellationToken: cancellationToken);
            groups.Add(group);
            tokenStream.Read();
        }
        return groups;
    }

    #endregion
    
    // https://drafts.csswg.org/css-syntax/#parser-algorithms
    #region Parser Algorithms

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-stylesheet-contents
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    public static List<Rule> ConsumeStylesheetContent(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        var rules = new List<Rule>();

        // Console.WriteLine("Parser started");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextToken = tokenStream.Peek();

            if (nextToken.Type is TokenType.Whitespace or TokenType.Comment or TokenType.Unknown or TokenType.CDO or TokenType.CDC) tokenStream.Read();
            if (nextToken.Type is TokenType.EOF) break;
            if (nextToken.Type is TokenType.AtKeyword)
            {
                var atRule = ConsumeAtRule(tokenStream, cancellationToken: cancellationToken);
                if (atRule is not null) rules.Add(atRule);
            }
            else
            {
                var qualifiedRule = ConsumeQualifiedRule(tokenStream, cancellationToken: cancellationToken);
                if (qualifiedRule is not null) rules.Add(qualifiedRule);
            }
        }
        // Console.WriteLine("Parser finished");
        
        return rules;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-at-rule
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="nested"></param>
    /// <param name="cancellationToken"></param>
    public static Rule? ConsumeAtRule(TokenStream tokenStream, bool nested = false, CancellationToken cancellationToken = default)
    {
        if (tokenStream.Peek().Type is not TokenType.AtKeyword)
            throw new InvalidOperationException("Token stream does not start with an at keyword.");
        
        var rule = new Rule(tokenStream.Read());

        while (true)
        {
            var nextToken = tokenStream.Peek();

            if (nextToken.Type is TokenType.Semicolon or TokenType.EOF)
            {
                rule.Tokens.Add(tokenStream.Read());
                
                // TODO if rule is valid in the current context, return it; otherwise return nothing.
                var ruleIsValid = true;
                if (ruleIsValid) break;
                return null;
            }
            if (nextToken.Type is TokenType.CurlyClose)
            {
                if (nested)
                {
                    // TODO if rule is valid in the current context, return it; otherwise return nothing.
                    var ruleIsValid = true;
                    if (ruleIsValid) break;
                    return null;
                }

                var token = tokenStream.Read();
                rule.Prelude.Add(new(token));
                rule.Tokens.Add(token);
            }
            else if (nextToken.Type is TokenType.CurlyOpen)
            {
                var block = ConsumeBlock(tokenStream, cancellationToken);
                rule.Declarations = block.Item1;
                rule.ChildRules = block.Item2;
                // TODO add to rules tokens
                
                // TODO if rule is valid in the current context, return it; otherwise return nothing.
                var ruleIsValid = true;
                if (ruleIsValid) break;
                return null;
            }
            else
            {
                var value = ConsumeComponentValue(tokenStream, cancellationToken);
                rule.Prelude.Add(value);
                rule.Tokens.AddRange(value.Tokens);
            }
        }
        
        return rule;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-qualified-rule
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="stopToken"></param>
    /// <param name="nested"></param>
    /// <param name="cancellationToken"></param>
    public static Rule? ConsumeQualifiedRule(TokenStream tokenStream, TokenType? stopToken = null, bool nested = false, CancellationToken cancellationToken = default)
    {
        var rule = new Rule();

        while (true)
        {
            var nextToken = tokenStream.Peek();

            if (nextToken.Type is TokenType.EOF || nextToken.Type == stopToken)
            {
                // This is a parse error
                return null;
            }

            if (nextToken.Type is TokenType.CurlyClose)
            {
                // This is a parse error
                if (nested) return null;
                var token = tokenStream.Read();
                rule.Prelude.Add(new(token));
                rule.Tokens.Add(token);
            }
            else if (nextToken.Type is TokenType.CurlyOpen)
            {
                // TODO If the first two non-<whitespace-token> values of rule’s prelude are an <ident-token> whose value starts with "--" followed by a <colon-token>
                var check = false;
                if (check)
                {
                    ConsumeBadDeclarationRemnants(tokenStream, nested, cancellationToken);
                    return null;
                }
                
                var block = ConsumeBlock(tokenStream, cancellationToken);
                rule.Declarations = block.Item1;
                rule.ChildRules = block.Item2;
                
                var ruleIsValid = true;
                if (ruleIsValid) break;
                return null;
            }
            else
            {
                var componentValue = ConsumeComponentValue(tokenStream, cancellationToken);
                rule.Prelude.Add(componentValue);
            }
        }
        
        return rule;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-block
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    public static (List<Declaration>, List<Rule>) ConsumeBlock(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        if (tokenStream.Peek().Type is not TokenType.CurlyOpen)
            throw new InvalidOperationException("Token stream does not start with an open bracket.");

        tokenStream.Read();
        var (decls, rules) = ConsumeBlockContent(tokenStream, cancellationToken);
        // TODO do I need to discard a token? https://drafts.csswg.org/css-syntax/#consume-stylesheet-contents:~:text=and%20rules.-,Discard%20a%20token%20from%20input.,-Return%20decls%20and
        return (decls, rules);
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-block-contents
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    public static (List<Declaration>, List<Rule>) ConsumeBlockContent(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        var decls = new List<Declaration>();
        var rules = new List<Rule>();

        while (true)
        {
            var nextToken = tokenStream.Peek();
            
            if (nextToken.Type is TokenType.Whitespace or TokenType.Semicolon) tokenStream.Read();
            else if (nextToken.Type is TokenType.EOF or TokenType.CurlyClose) break;
            else if (nextToken.Type is TokenType.AtKeyword)
            {
                var atRule = ConsumeAtRule(tokenStream, true, cancellationToken);
                if (atRule is not null) rules.Add(atRule);
            }
            else
            {
                tokenStream.Mark();
                var decl = ConsumeDeclaration(tokenStream, true, cancellationToken);
                if (decl is not null)
                {
                    tokenStream.DiscardMark();
                    decls.Add(decl);
                }
                else
                {
                    tokenStream.RestoreMark();
                    var qualifiedRule = ConsumeQualifiedRule(tokenStream, TokenType.Semicolon, true, cancellationToken);
                    if (qualifiedRule is not null) rules.Add(qualifiedRule);
                }
            }
        }
        
        return (decls, rules);
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-declaration
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="nested"></param>
    /// <param name="cancellationToken"></param>
    public static Declaration? ConsumeDeclaration(TokenStream tokenStream, bool nested = false, CancellationToken cancellationToken = default)
    {
        Declaration decl;

        var nextToken = tokenStream.Peek();

        if (nextToken.Type is TokenType.Ident)
        {
            var token = tokenStream.Read();
            decl = new(token);
        }
        else
        {
            // TODO Maintain declaration remnants tokens
            ConsumeBadDeclarationRemnants(tokenStream, nested, cancellationToken);
            return null;
        }

        DiscardWhitespace(tokenStream, cancellationToken);
        
        if (tokenStream.Peek().Type is TokenType.Colon)
        {
            var token = tokenStream.Read();
            decl.Tokens.Add(token);
        }
        else
        {
            ConsumeBadDeclarationRemnants(tokenStream, nested, cancellationToken);
            return null;
        }
        
        var list = ConsumeComponentValues(tokenStream, TokenType.Semicolon, nested, cancellationToken);
        decl.Value = list;

        // If is custom property name string
        if (decl.Name.StartsWith("--"))
        {
            // TODO should originalText only be taking the tokens of list or any componentValue?
            var originalText = new StringBuilder();
            foreach (var valueToken in list.SelectMany(value => value.Tokens))
            {
                originalText.Append(valueToken.Text);
            }
        }
        else if (decl.Name.ToLowerInvariant() == "unicode-range")
        {
            // TODO  consume the value of a unicode-range descriptor from the segment of the original source text string corresponding to the tokens returned by the consume a list of component values call, and replace decl’s value with the result.
        }
        
        var nonWhitespaceValues = decl.Value.Where(x => !x.IsToken || ((Token) x.Value!).Type is not TokenType.Whitespace).ToList();
        if (nonWhitespaceValues.Count > 2)
        {
            var potentialExclamationMark = nonWhitespaceValues[^2];
            var potentialImportantIdent = nonWhitespaceValues[^1];
            if (potentialExclamationMark is { IsToken: true, AsToken: { Type: TokenType.Delim, Value: "!" } })
            {
                if (potentialImportantIdent is { IsToken: true, AsToken: { Type: TokenType.Ident, Value: "important" } })
                {
                    // TODO ensure the == operator works as expected
                    var exclamationIndex = decl.Value.FindLastIndex(x => x == potentialExclamationMark);
                    var importantIdentIndex = decl.Value.FindLastIndex(x => x == potentialImportantIdent);
                    decl.Value.RemoveAt(exclamationIndex);
                    decl.Value.RemoveAt(importantIdentIndex);
                    decl.Important = true;
                }
            }
        }

        while (true)
        {
            var lastValue = decl.Value.Last();
            if (lastValue is { IsToken: true, AsToken.Type: TokenType.Whitespace })
            {
                decl.Value.RemoveAt(decl.Value.Count - 1);
            }
            else
            {
                break;
            }
        }
        
        // TODO If decl is valid in the current context, return it; otherwise return nothing.
        var validInContext = true;

        return validInContext ? decl : null;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-list-of-components
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="stopToken"></param>
    /// <param name="nested"></param>
    /// <param name="cancellationToken"></param>
    public static List<ComponentValue> ConsumeComponentValues(TokenStream tokenStream, TokenType? stopToken = null, bool nested = false, CancellationToken cancellationToken = default)
    {
        var values = new List<ComponentValue>();

        while (true)
        {
            var nextToken = tokenStream.Peek();
            
            if (nextToken.Type is TokenType.EOF || nextToken.Type == stopToken) break;
            if (nextToken.Type is TokenType.CurlyClose)
            {
                if (nested) break;
                
                // This is a parse error
                var token = tokenStream.Read();
                values.Add(new(token));
            }
            else
            {
                var value = ConsumeComponentValue(tokenStream, cancellationToken);
                values.Add(value);
            }
        }

        return values;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-component-value
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ComponentValue ConsumeComponentValue(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        var nextToken = tokenStream.Peek();
        return nextToken.Type switch
        {
            TokenType.CurlyOpen or TokenType.ParenOpen or TokenType.SquareOpen => new(ConsumeSimpleBlock(tokenStream, cancellationToken)),
            TokenType.Function => new(ConsumeFunction(tokenStream, cancellationToken)),
            _ => new(tokenStream.Read())
        };
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-simple-block
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Block ConsumeSimpleBlock(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        if (tokenStream.Peek().Type is not TokenType.CurlyOpen and not TokenType.ParenOpen and not TokenType.SquareOpen)
            throw new InvalidOperationException("Token stream does not start with an open bracket.");

        var startToken = tokenStream.Read();
        var block = new Block(startToken);

        while (true)
        {
            var nextToken = tokenStream.Peek();

            if (nextToken.Type is TokenType.EOF) break;
            if (nextToken.Type == startToken.GetMirrorVariant())
            {
                block.Tokens.Add(tokenStream.Read());
                break;
            }
            
            var componentValue = ConsumeComponentValue(tokenStream, cancellationToken);
            block.Value.Add(componentValue);
            // block.Tokens.AddRange(componentValue.Tokens);
        }
        
        return block;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-function
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Function ConsumeFunction(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        if (tokenStream.Peek().Type is not TokenType.Function)
            throw new InvalidOperationException("Token stream does not start with a function.");

        var functionToken = tokenStream.Read();
        var function = new Function(functionToken);

        while (true)
        {
            var nextToken = tokenStream.Peek();

            if (nextToken.Type is TokenType.EOF) break;

            if (nextToken.Type is TokenType.ParenClose)
            {
                function.Tokens.Add(tokenStream.Read());
                break;
            }
            
            var componentValue = ConsumeComponentValue(tokenStream, cancellationToken);
            function.Value.Add(componentValue);
            // function.Tokens.AddRange(componentValue.Tokens);
        }

        return function;
    }

    // TODO ConsumeUnicodeRangeValue

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#token-stream-discard-whitespace
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static List<Token> DiscardWhitespace(TokenStream tokenStream, CancellationToken cancellationToken = default)
    {
        var list = new List<Token>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextToken = tokenStream.Peek();
            if (nextToken.Type is not TokenType.Whitespace) break;
            list.Add(tokenStream.Read());
        }

        return list;
    }

    /// <summary>
    /// https://drafts.csswg.org/css-syntax/#consume-the-remnants-of-a-bad-declaration
    /// </summary>
    /// <param name="tokenStream"></param>
    /// <param name="nested"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static List<Token> ConsumeBadDeclarationRemnants(TokenStream tokenStream, bool nested = false, CancellationToken cancellationToken = default)
    {
        var tokens = new List<Token>();

        while (true)
        {
            var nextToken = tokenStream.Peek();
            if (nextToken.Type is TokenType.EOF) break;
            if (nextToken.Type is TokenType.Semicolon)
            {
                tokens.Add(tokenStream.Read());
                break;
            }
            if (nextToken.Type is TokenType.CurlyClose)
            {
                if (nested) break;
                tokens.Add(tokenStream.Read());
            }
            else
            {
                var componentValue = ConsumeComponentValue(tokenStream, cancellationToken);
                tokens.AddRange(componentValue.Tokens);
            }
        }

        return tokens;
    }
    
    #endregion

    public void Dispose()
    {
        Tokenizer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return Tokenizer?.DisposeAsync() ?? default;
    }
}