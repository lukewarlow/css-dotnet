using CSSDotNet.Parser;
using FluentAssertions;
using Xunit.Abstractions;

namespace Test;

public class TokenizerTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly CancellationToken _cancellationToken;

    public TokenizerTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _cancellationToken = new CancellationTokenSource(5000).Token;
        
    }

    [Fact]
    public async void FullComment()
    {
        await using var tokenizer = new Tokenizer("/* comment */");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Comment);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void BrokenComment()
    {
        await using var tokenizer = new Tokenizer("/* comment");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Comment);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void WhitespaceNormalisedIntoSingleToken()
    {
        await using var tokenizer = new Tokenizer(" \t\n \r \f ");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Whitespace);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #region string tests

    [Fact]
    public async void StringWithQuotes()
    {
        await using var tokenizer = new Tokenizer("\"hello\"");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithApostrophes()
    {
        await using var tokenizer = new Tokenizer("'hello'");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithMismatch()
    {
        // Parse error but still returns a string token
        await using var tokenizer = new Tokenizer("'hello\"");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithMissingEnd()
    {
        // Parse error but still returns a string token
        await using var tokenizer = new Tokenizer("'hello");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithReverseSolidus()
    {
        await using var tokenizer = new Tokenizer("'hello\\");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithEscapedNewLine()
    {
        await using var tokenizer = new Tokenizer("'hello\\\nworld'");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void StringWithEscape()
    {
        await using var tokenizer = new Tokenizer("'\\75'");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.String);
        tokens.First().Value.Should().Be("u");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void BadString()
    {
        await using var tokenizer = new Tokenizer("'hello\n'");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(3);
        tokens.First().Type.Should().Be(TokenType.BadString);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    #region hash tests

    [Fact]
    public async void HashWithIdent()
    {
        await using var tokenizer = new Tokenizer("#foobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Hash);
        tokens.First().Value.Should().Be("foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void HashWithEscapedIdent()
    {
        await using var tokenizer = new Tokenizer("#\\66oobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Hash);
        tokens.First().Value.Should().Be("foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void HashDelim()
    {
        await using var tokenizer = new Tokenizer("#");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    [Fact]
    public async void ParenOpen()
    {
        await using var tokenizer = new Tokenizer("(");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.ParenOpen);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void ParenClose()
    {
        await using var tokenizer = new Tokenizer(")");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.ParenClose);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void PlusNumber()
    {
        await using var tokenizer = new Tokenizer("+123");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().Be('+');
        tokens.First().Number!.Value.Should().Be(123);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void PlusDelim()
    {
        await using var tokenizer = new Tokenizer("+");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Comma()
    {
        await using var tokenizer = new Tokenizer(",");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Comma);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #region minus tests

    [Fact]
    public async void MinusNumber()
    {
        await using var tokenizer = new Tokenizer("-123");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().Be('-');
        tokens.First().Number!.Type.Should().Be("integer");
        // TODO spec is unclear if the value should be unsigned or not. The sign is stored so potentially it should be?
        tokens.First().Number!.Value.Should().Be(-123);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void EndLegacyComment()
    {
        await using var tokenizer = new Tokenizer("-->");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.CDC);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void CustomProperty()
    {
        await using var tokenizer = new Tokenizer("--foobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("--foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void CustomPropertyWithEscape()
    {
        await using var tokenizer = new Tokenizer("--\\66oobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("--foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void MinusDelim()
    {
        await using var tokenizer = new Tokenizer("-");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    #region full stop tests

    [Fact]
    public async void DecimalNumberHangingDot()
    {
        await using var tokenizer = new Tokenizer(".5");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("number");
        tokens.First().Number!.Value.Should().Be(0.5);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void FullStopDelim()
    {
        await using var tokenizer = new Tokenizer(".");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    [Fact]
    public async void Colon()
    {
        await using var tokenizer = new Tokenizer(":");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Colon);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Semicolon()
    {
        await using var tokenizer = new Tokenizer(";");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Semicolon);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #region less than tests

    [Fact]
    public async void StartLegacyComment()
    {
        await using var tokenizer = new Tokenizer("<!--");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.CDO);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void LessThanDelim()
    {
        await using var tokenizer = new Tokenizer("<");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    #region at tests

    [Fact]
    public async void AtRule()
    {
        await using var tokenizer = new Tokenizer("@foobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.AtKeyword);
        tokens.First().Value.Should().Be("foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void AtRuleWithEscape()
    {
        await using var tokenizer = new Tokenizer("@\\66oobar");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.AtKeyword);
        tokens.First().Value.Should().Be("foobar");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void AtDelim()
    {
        await using var tokenizer = new Tokenizer("@");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    [Fact]
    public async void SquareOpen()
    {
        await using var tokenizer = new Tokenizer("[");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.SquareOpen);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void EscapedChar()
    {
        // This is a parse error but will still return a delim token
        await using var tokenizer = new Tokenizer("\\66");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("f");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void ReverseSolidusDelim()
    {
        // This is a parse error but will still return a delim token
        await using var tokenizer = new Tokenizer("\\");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void SquareClose()
    {
        await using var tokenizer = new Tokenizer("]");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.SquareClose);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void CurlyOpen()
    {
        await using var tokenizer = new Tokenizer("{");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.CurlyOpen);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void CurlyClose()
    {
        await using var tokenizer = new Tokenizer("}");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.CurlyClose);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #region digit tests

    [Fact]
    public async void Integer()
    {
        await using var tokenizer = new Tokenizer("123");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("integer");
        tokens.First().Number!.Value.Should().Be(123);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Decimal()
    {
        await using var tokenizer = new Tokenizer("123.123");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("number");
        tokens.First().Number!.Value.Should().Be(123.123);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void NumberWithExponent()
    {
        await using var tokenizer = new Tokenizer("10e3");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("number");
        tokens.First().Number!.Value.Should().Be(10000);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void NumberWithPositiveExponent()
    {
        await using var tokenizer = new Tokenizer("10e+3");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("number");
        tokens.First().Number!.Value.Should().Be(10000);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void NumberWithNegativeExponent()
    {
        await using var tokenizer = new Tokenizer("10e-3");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Number);
        tokens.First().Number!.Sign.Should().BeNull();
        tokens.First().Number!.Type.Should().Be("number");
        tokens.First().Number!.Value.Should().Be(0.01);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    // Unicode range tests

    [Fact]
    public async void LowerU()
    {
        await using var tokenizer = new Tokenizer("u");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("u");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void UpperU()
    {
        await using var tokenizer = new Tokenizer("U");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("U");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #region ident-start code point tests

    [Fact]
    public async void Letters()
    {
        await using var tokenizer = new Tokenizer("abc");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("abc");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void NonAsciiCodePoint()
    {
        await using var tokenizer = new Tokenizer("·");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("·");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void NonAsciiCodePointStart()
    {
        await using var tokenizer = new Tokenizer("·abc");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("·abc");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Underscore()
    {
        await using var tokenizer = new Tokenizer("_");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("_");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void UnderscoreStart()
    {
        await using var tokenizer = new Tokenizer("_abc");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Ident);
        tokens.First().Value.Should().Be("_abc");
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion

    [Fact]
    public async void EndOfFile()
    {
        await using var tokenizer = new Tokenizer("");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(1);
        tokens.First().Type.Should().Be(TokenType.EOF);
    }

    #region Anything else delims

    [Fact]
    public async void GreaterThan()
    {
        await using var tokenizer = new Tokenizer(">");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Tilda()
    {
        await using var tokenizer = new Tokenizer("~");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    [Fact]
    public async void Solidus()
    {
        await using var tokenizer = new Tokenizer("/");
        var tokens = tokenizer.Tokenize(_cancellationToken).ToList();
        tokens.Count.Should().Be(2);
        tokens.First().Type.Should().Be(TokenType.Delim);
        tokens.Last().Type.Should().Be(TokenType.EOF);
    }

    #endregion
}