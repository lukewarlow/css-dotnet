using System.Text;
using CSSDotNet.Utils;

namespace CSSDotNet.Parser;

public class Tokenizer : IDisposable, IAsyncDisposable
{
    private Stream CssStream { get; }

    public Tokenizer(string cssText)
    {
        CssStream = new MemoryStream(Encoding.UTF8.GetBytes(cssText));
    }

    public Tokenizer(byte[] cssByteArray)
    {
        // TODO validate that the byte array is valid UTF-8
        CssStream = new MemoryStream(cssByteArray);
    }

    public Tokenizer(Stream cssStream)
    {
        CssStream = cssStream;
    }

    public IEnumerable<Token> Tokenize(CancellationToken cancellationToken = default)
    {
        // Console.WriteLine("CSS Tokenizer started");
        using var reader = new BufferedStreamReader(CssStream);
        reader.Init();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = ConsumeToken(reader, cancellationToken: cancellationToken);
            if (token.Type == TokenType.EOF)
            {
                // Console.WriteLine("CSS Tokenizer finished");
                yield return token;
                yield break;
            }

            yield return token;
        }
    }

    static TokenType? CharToTokenType(char c)
    {
        return c switch
        {
            '{' => TokenType.CurlyOpen,
            '}' => TokenType.CurlyClose,
            '[' => TokenType.SquareOpen,
            ']' => TokenType.SquareClose,
            '(' => TokenType.ParenOpen,
            ')' => TokenType.ParenClose,
            ',' => TokenType.Comma,
            ':' => TokenType.Colon,
            ';' => TokenType.Semicolon,
            '\'' or '"' => TokenType.String,
            ' ' or '\n' or '\t' => TokenType.Whitespace,
            _ => null,
        };
    }

    public static Token ConsumeToken(BufferedStreamReader reader, bool unicodeRangesAllowed = false,
        CancellationToken cancellationToken = default)
    {
        var nextCharVal = reader.Peek();
        var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
        Token? result = null;
        if (nextChar == null)
        {
            result = new(TokenType.EOF)
            {
                Start = new()
                {
                    Line = reader.Line,
                    Column = reader.Column,
                }
            };
        }
        else
        {
            var tokenType = CharToTokenType(nextChar.Value);

            if (tokenType == TokenType.Whitespace)
            {
                result = ConsumeWhitespaceToken(reader);
            }
            else if (tokenType == TokenType.String)
            {
                result = ConsumeStringToken(reader);
            }
            else if (tokenType == null)
            {
                if (nextChar == '/')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    result = nextNextChar == '*' ? ConsumeComment(reader) : ConsumeDelimToken(reader);
                }
                else if (nextChar == '#')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    var nextNextNextCharVal = reader.PeekThree();
                    var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;
                    if (nextNextChar.HasValue && (IsIdentCodePoint(nextNextChar.Value) ||
                                                  AreTwoCodePointsAValidEscape(nextNextChar.Value, nextNextNextChar)))
                    {
                        result = ConsumeHashToken(reader);
                    }
                    else
                    {
                        result = ConsumeDelimToken(reader);
                    }
                }
                else if (nextChar == '-')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    var nextNextNextCharVal = reader.PeekThree();
                    var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;
                    // TODO rename nextNextChar and nextNextNextChar to be more descriptive, potentially match the spec
                    if (StartsANumber(nextChar.Value, nextNextChar, nextNextNextChar))
                    {
                        result = ConsumeNumericToken(reader, cancellationToken);
                    }
                    else if (nextNextChar == '-' && nextNextNextChar == '>')
                    {
                        result = new(TokenType.CDC, "-->")
                        {
                            Start = new()
                            {
                                Line = reader.Line,
                                Column = reader.Column,
                            },
                        };
                        reader.Read();
                        reader.Read();
                        reader.Read();
                        result.End = new()
                        {
                            Line = reader.Line,
                            Column = reader.Column,
                        };
                    }
                    else if (StartsAnIdentSequence(nextChar.Value, nextNextChar, nextNextNextChar))
                    {
                        result = ConsumeIdentLikeToken(reader, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        result = ConsumeDelimToken(reader);
                    }
                }
                else if (nextChar is '+' or '.')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    var nextNextNextCharVal = reader.PeekThree();
                    var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;
                    result = StartsANumber(nextChar.Value, nextNextChar, nextNextNextChar)
                        ? ConsumeNumericToken(reader, cancellationToken)
                        : ConsumeDelimToken(reader);
                }
                else if (nextChar == '<')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    var nextNextNextCharVal = reader.PeekThree();
                    var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;
                    var nextNextNextNextCharVal = reader.PeekFour();
                    var nextNextNextNextChar = nextNextNextNextCharVal == -1 ? null : (char?)nextNextNextNextCharVal;
                    if (nextNextChar == '!' && nextNextNextChar == '-' && nextNextNextNextChar == '-')
                    {
                        result = new(TokenType.CDO, "<!--")
                        {
                            Start = new()
                            {
                                Line = reader.Line,
                                Column = reader.Column,
                            },
                        };
                        reader.Read();
                        reader.Read();
                        reader.Read();
                        reader.Read();
                        result.End = new()
                        {
                            Line = reader.Line,
                            Column = reader.Column,
                        };
                    }
                    else
                    {
                        result = ConsumeDelimToken(reader);
                    }
                }
                else if (nextChar == '@')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
                    var nextNextNextCharVal = reader.PeekThree();
                    var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;
                    var nextNextNextNextCharVal = reader.PeekFour();
                    var nextNextNextNextChar = nextNextNextNextCharVal == -1 ? null : (char?)nextNextNextNextCharVal;

                    if (nextNextChar.HasValue &&
                        StartsAnIdentSequence(nextNextChar.Value, nextNextNextChar, nextNextNextNextChar))
                    {
                        result = new(TokenType.AtKeyword, "@", "@")
                        {
                            Start = new()
                            {
                                Line = reader.Line,
                                Column = reader.Column,
                            },
                        };

                        var identSequence = ConsumeIdentSequence(reader, cancellationToken: cancellationToken);
                        result.Value = identSequence;
                        result.TextBuilder.Append(identSequence);
                        result.End.Line = reader.Line;
                        result.End.Column = reader.Column;
                    }
                    else
                    {
                        result = ConsumeDelimToken(reader);
                    }
                }
                else if (nextChar == '\\')
                {
                    var nextNextCharVal = reader.PeekTwo();
                    var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

                    if (AreTwoCodePointsAValidEscape(nextChar.Value, nextNextChar))
                    {
                        var escapedCodePoint = ConsumeEscapedCodePoint(reader, cancellationToken);
                        result = ConsumeIdentLikeToken(reader, escapedCodePoint, cancellationToken);
                    }
                    else
                    {
                        result = ConsumeDelimToken(reader);
                    }
                }
                else if (nextChar.Value is 'u' or 'U')
                {
                    // TODO would start unicode range check
                    var wouldStartUnicodeRange = false;
                    if (unicodeRangesAllowed && wouldStartUnicodeRange)
                    {
                        // TODO unicode ranges
                    }
                    else
                    {
                        result = ConsumeIdentLikeToken(reader, cancellationToken: cancellationToken);
                    }
                }
                else if (char.IsAsciiDigit(nextChar.Value))
                {
                    result = ConsumeNumericToken(reader, cancellationToken);
                }
                else if (IsIdentStartCodePoint(nextChar.Value))
                {
                    result = ConsumeIdentLikeToken(reader, cancellationToken: cancellationToken);
                }
                else
                {
                    result = ConsumeDelimToken(reader);
                }

                result ??= ConsumeUnknownToken(reader, cancellationToken);
            }
            else
            {
                result = new(tokenType.Value, nextChar.Value.ToString(), nextChar.Value.ToString())
                {
                    Start = new()
                    {
                        Line = reader.Line,
                        Column = reader.Column,
                    },
                };
                reader.Read();
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
            }
        }

        return result;
    }

    /// <summary>
    /// This is not fully following the spec, but it's useful to keep comments in the token stream
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static Token ConsumeComment(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var result = new Token(TokenType.Comment, "/*", "")
        {
            Start = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            },
            End = new()
            {
                Line = reader.Line,
            }
        };
        reader.Read();
        reader.Read();

        result.End.Column = reader.Column;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            if (nextChar == null) break;
            if (nextChar != '*' || reader.PeekTwo() != '/')
            {
                result.ValueBuilder.Append(nextChar.Value);
                result.TextBuilder.Append(nextChar.Value);
                reader.Read();
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
                continue;
            }

            result.TextBuilder.Append("*/");
            reader.Read();
            reader.Read();
            result.End.Line = reader.Line;
            result.End.Column = reader.Column;
            break;
        }

        return result;
    }

    static Token ConsumeWhitespaceToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var startLine = reader.Line;
        var startColumn = reader.Column;
        var initialChar = (char)reader.Read();
        var result = new Token(TokenType.Whitespace, initialChar.ToString(), initialChar.ToString())
        {
            Start = new()
            {
                Line = startLine,
                Column = startColumn,
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            }
        };

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

            if (nextChar == null) break;

            var nextCharTokenType = CharToTokenType(nextChar.Value);
            if (nextCharTokenType != TokenType.Whitespace) break;

            result.ValueBuilder.Append(nextChar.Value);
            result.TextBuilder.Append(nextChar.Value);
            reader.Read();
            result.End.Line = reader.Line;
            result.End.Column = reader.Column;
        }

        return result;
    }

    public static Token ConsumeStringToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var startLine = reader.Line;
        var startColumn = reader.Column;
        var initialChar = (char)reader.Read();
        var result = new Token(TokenType.String, initialChar.ToString())
        {
            Start = new()
            {
                Line = startLine,
                Column = startColumn,
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            }
        };

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

            if (nextChar == null) break;

            if (nextChar.Value == initialChar)
            {
                reader.Read();
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
                break;
            }

            if (nextChar.Value is '\n')
            {
                reader.Read();
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
                result.Type = TokenType.BadString;
                break;
            }

            if (nextChar.Value is '\\')
            {
                var nextNextCharVal = reader.PeekTwo();
                var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

                if (nextNextChar is '\n')
                {
                    result.TextBuilder.Append(nextNextChar);
                    reader.Read();
                    reader.Read();
                    result.End.Line = reader.Line;
                    result.End.Column = reader.Column;
                }
                else if (AreTwoCodePointsAValidEscape(nextChar.Value, nextNextChar))
                {
                    var escapedCodePoint = ConsumeEscapedCodePoint(reader, cancellationToken);
                    result.ValueBuilder.Append(escapedCodePoint);
                    result.TextBuilder.Append(escapedCodePoint);
                    result.End.Line = reader.Line;
                    result.End.Column = reader.Column;
                }
                else
                {
                    result.TextBuilder.Append(nextNextChar);
                    reader.Read();
                    result.End.Line = reader.Line;
                    result.End.Column = reader.Column;
                }
            }
            else
            {
                reader.Read();
                result.ValueBuilder.Append(nextChar);
                result.TextBuilder.Append(nextChar);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
            }
        }

        return result;
    }

    static Token ConsumeHashToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var initialChar = reader.Peek();
        var result = new Token(TokenType.Hash, initialChar.ToString())
        {
            Start = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            },
        };

        // TODO if the next 3 code points would start an ident sequence, set the a type flag to ID

        var identSequence = ConsumeIdentSequence(reader, cancellationToken: cancellationToken);
        result.Value = identSequence;
        result.TextBuilder.Append(identSequence);
        result.End.Line = reader.Line;
        result.End.Column = reader.Column;

        return result;
    }

    static Token ConsumeNumericToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var startLine = reader.Line;
        var startColumn = reader.Column;
        Token? result = null;
        var number = ConsumeNumber(reader, cancellationToken);
        var nextCharVal = reader.Peek();
        var nextNextCharVal = reader.PeekTwo();
        var nextNextNextCharVal = reader.PeekThree();
        var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
        var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
        var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;

        if (nextChar.HasValue)
        {
            // reader.Read(); TODO should this be here or inside the if statements?
            if (StartsAnIdentSequence(nextChar.Value, nextNextChar, nextNextNextChar))
            {
                reader.Read();
                var identSequence = ConsumeIdentSequence(reader, nextChar.Value, cancellationToken);
                result = new(TokenType.Dimension, number.Text! + identSequence, identSequence)
                {
                    Start = new()
                    {
                        Line = startLine,
                        Column = startColumn,
                    },
                    End = new()
                    {
                        Line = reader.Line,
                        Column = reader.Column,
                    },
                };
            }
            else if (nextChar == '%')
            {
                reader.Read();
                result = new(TokenType.Percentage, number.Text! + '%', "%")
                {
                    Start = new()
                    {
                        Line = startLine,
                        Column = startColumn,
                    },
                    End = new()
                    {
                        Line = reader.Line,
                        Column = reader.Column,
                    }
                };
            }
        }

        result ??= new(TokenType.Number, number.Text!)
        {
            Start = new()
            {
                Line = startLine,
                Column = startColumn,
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            },
        };

        result.Number = number;

        return result;
    }

    static Token ConsumeIdentLikeToken(BufferedStreamReader reader, char? escapedCodePoint = null,
        CancellationToken cancellationToken = default)
    {
        Token? result = null;
        // TODO these wont quite be right if there's an escaped code point
        var line = reader.Line;
        var column = reader.Column;
        var identSequence = ConsumeIdentSequence(reader, escapedCodePoint, cancellationToken);
        if (identSequence.Equals("url", StringComparison.InvariantCultureIgnoreCase))
        {
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            if (nextChar == '(')
            {
                reader.Read();
                var nextNextCharVal = reader.Peek();
                var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

                // TODO this isn't fully matching the spec behaviour yet
                if (nextNextChar is '"' or '\'')
                {
                    result = new(TokenType.Function, identSequence + '(', identSequence + '(')
                    {
                        Start = new()
                        {
                            Line = line,
                            Column = column
                        },
                        End = new()
                        {
                            Line = reader.Line,
                            Column = reader.Column,
                        }
                    };
                }
                else
                {
                    result = ConsumeUrlToken(reader, cancellationToken);
                    result.Start = new()
                    {
                        Line = line,
                        Column = column
                    };
                }
            }
        }
        else
        {
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            if (nextChar == '(')
            {
                reader.Read();
                result = new(TokenType.Function, identSequence + '(', identSequence + '(')
                {
                    Start = new()
                    {
                        Line = line,
                        Column = column
                    },
                    End = new()
                    {
                        Line = reader.Line,
                        Column = reader.Column,
                    }
                };
            }
        }

        result ??= new(TokenType.Ident, identSequence, identSequence)
        {
            Start = new()
            {
                Line = reader.Line,
                Column = reader.Column
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column + identSequence.Length,
            }
        };

        return result;
    }

    static Token ConsumeUrlToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var result = new Token(TokenType.Url, "url(", "");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

            if (!nextChar.HasValue) break;

            if (CharToTokenType(nextChar.Value) == TokenType.Whitespace)
            {
                reader.Read();
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
            }
            else break;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            var nextNextCharVal = reader.Peek();
            var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

            if (nextChar is ')')
            {
                reader.Read();
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
                break;
            }

            if (!nextChar.HasValue) break;

            if (CharToTokenType(nextChar.Value) == TokenType.Whitespace)
            {
                reader.Read();
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
                if (nextNextChar.HasValue && nextNextChar != ')' &&
                    CharToTokenType(nextNextChar.Value) != TokenType.Whitespace)
                {
                    var badUrlRemnants = ConsumeBadUrlRemnants(reader, cancellationToken);
                    result = new(TokenType.BadUrl, result.Text + badUrlRemnants)
                    {
                        End =
                        {
                            Line = reader.Line,
                            Column = reader.Column
                        }
                    };

                    break;
                }
            }
            else if (nextChar is '"' or '\'' or '(') // TODO handle non-printable code points
            {
                var badUrlRemnants = ConsumeBadUrlRemnants(reader, cancellationToken);
                result = new(TokenType.BadUrl, result.Text + badUrlRemnants)
                {
                    End =
                    {
                        Line = reader.Line,
                        Column = reader.Column
                    }
                };

                break;
            }
            else if (nextChar is '\\')
            {
                if (AreTwoCodePointsAValidEscape(nextChar.Value, nextNextChar))
                {
                    var escapedCodePoint = ConsumeEscapedCodePoint(reader, cancellationToken);
                    result.ValueBuilder.Append(escapedCodePoint);
                    result.TextBuilder.Append(escapedCodePoint);
                    result.End.Line = reader.Line;
                    result.End.Column = reader.Column;

                    break;
                }

                var badUrlRemnants = ConsumeBadUrlRemnants(reader, cancellationToken);
                result = new(TokenType.BadUrl, result.Text + badUrlRemnants)
                {
                    End =
                    {
                        Line = reader.Line,
                        Column = reader.Column
                    }
                };

                break;
            }
            else
            {
                reader.Read();
                result.ValueBuilder.Append(nextChar.Value);
                result.TextBuilder.Append(nextChar.Value);
                result.End.Line = reader.Line;
                result.End.Column = reader.Column;
            }
        }

        return result;
    }

    static Token ConsumeDelimToken(BufferedStreamReader reader)
    {
        var value = (char)reader.Read();
        var result = new Token(TokenType.Delim, value.ToString(), value.ToString())
        {
            Start = new()
            {
                Line = reader.Line,
                Column = reader.Column - 1,
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column,
            }
        };

        return result;
    }

    static Token ConsumeUnknownToken(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var startLine = reader.Line;
        var startColumn = reader.Column;
        var initialChar = (char)reader.Read();
        var result = new Token(TokenType.Unknown, initialChar.ToString(), initialChar.ToString())
        {
            Start = new()
            {
                Line = startLine,
                Column = startColumn
            },
            End = new()
            {
                Line = reader.Line,
                Column = reader.Column
            }
        };

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentCharVal = reader.Read();
            var currentChar = currentCharVal == -1 ? null : (char?)currentCharVal;
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

            if (currentChar == null) break;

            result.ValueBuilder.Append(currentChar.Value);
            result.TextBuilder.Append(currentChar.Value);
            result.End.Line = reader.Line;
            // TODO check this is correct
            result.End.Column = reader.Column;

            if (nextChar == null) break;

            var nextCharTokenType = CharToTokenType(nextChar.Value);
            if (nextCharTokenType != null) break;
        }

        return result;
    }

    static string ConsumeIdentSequence(BufferedStreamReader reader, char? escapedCodePoint = null,
        CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();

        var initialChar = escapedCodePoint ?? (char)reader.Read();

        if (IsIdentCodePoint(initialChar))
            result.Append(initialChar);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            var nextNextCharVal = reader.PeekTwo();
            var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

            if (nextChar == null) break;

            if (IsIdentCodePoint(nextChar.Value))
            {
                reader.Read();
                result.Append(nextChar);
            }
            else if (AreTwoCodePointsAValidEscape(nextChar.Value, nextNextChar))
            {
                result.Append(ConsumeEscapedCodePoint(reader));
            }
            else
            {
                break;
            }
        }

        return result.ToString();
    }

    public static Number ConsumeNumber(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();
        var type = "integer";
        var numberPart = new StringBuilder();
        var exponentPart = new StringBuilder();
        char? signCharacter = null;

        var nextCharVal = reader.Peek();
        var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

        if (nextChar is '+' or '-')
        {
            signCharacter = nextChar;
            numberPart.Append(nextChar);
            text.Append(nextChar);
            reader.Read();
        }

        var digitsResult = ConsumeDigits(reader, cancellationToken);
        numberPart.Append(digitsResult);
        text.Append(digitsResult);

        nextCharVal = reader.Peek();
        nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
        var nextNextCharVal = reader.PeekTwo();
        var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

        if (nextChar == '.' && nextNextChar.HasValue && char.IsAsciiDigit(nextNextChar.Value))
        {
            numberPart.Append(nextChar);
            text.Append(nextChar);
            reader.Read();

            var result = ConsumeDigits(reader, cancellationToken);
            numberPart.Append(result);
            text.Append(result);
            type = "number";
        }

        nextCharVal = reader.Peek();
        nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
        nextNextCharVal = reader.PeekTwo();
        nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
        var nextNextNextCharVal = reader.PeekThree();
        var nextNextNextChar = nextNextNextCharVal == -1 ? null : (char?)nextNextNextCharVal;

        if (nextChar is 'e' or 'E' &&
            ((nextNextChar is '-' or '+' && nextNextNextChar.HasValue && char.IsAsciiDigit(nextNextNextChar.Value)) ||
             nextNextChar.HasValue && char.IsAsciiDigit(nextNextChar.Value)))
        {
            Console.WriteLine("IsExponent");
            reader.Read();
            text.Append(nextChar);
            if (nextNextChar is '+' or '-')
            {
                exponentPart.Append(nextNextChar);
                text.Append(nextNextChar);
                reader.Read();
            }

            var result = ConsumeDigits(reader, cancellationToken);
            exponentPart.Append(result);
            text.Append(result);

            type = "number";
        }

        var value = double.Parse(numberPart.ToString());
        if (exponentPart.Length > 0)
        {
            value *= Math.Pow(10, double.Parse(exponentPart.ToString()));
        }

        return new(type, text.ToString(), value, signCharacter);
    }

    private static StringBuilder ConsumeDigits(BufferedStreamReader reader,
        CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

            if (nextChar == null) break;

            if (char.IsAsciiDigit(nextChar.Value))
            {
                result.Append(nextChar);
                reader.Read();
            }
            else break;
        }

        return result;
    }

    public static string ConsumeBadUrlRemnants(BufferedStreamReader reader,
        CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextCharVal = reader.Peek();
            var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;
            var nextNextCharVal = reader.PeekTwo();
            var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;

            if (!nextChar.HasValue) break;

            if (AreTwoCodePointsAValidEscape(nextChar.Value, nextNextChar))
            {
                var escapedCodePoint = ConsumeEscapedCodePoint(reader, cancellationToken);
                result.Append(escapedCodePoint);
            }
            else
            {
                reader.Read();
                result.Append(nextChar.Value);

                if (nextChar == ')') break;
            }
        }

        return result.ToString();
    }

    // TODO update to correctly capture and return the text value
    static char ConsumeEscapedCodePoint(BufferedStreamReader reader, CancellationToken cancellationToken = default)
    {
        reader.Read();
        var nextCharVal = reader.Read();
        var nextChar = nextCharVal == -1 ? null : (char?)nextCharVal;

        if (!nextChar.HasValue) return '\uFFFD';
        if (!char.IsAsciiHexDigit(nextChar.Value)) return nextChar.Value;

        var hexString = new StringBuilder(nextChar.Value.ToString());
        var hexCount = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextNextCharVal = reader.Peek();
            var nextNextChar = nextNextCharVal == -1 ? null : (char?)nextNextCharVal;
            if (!nextNextChar.HasValue) break;
            if (char.IsAsciiHexDigit(nextNextChar.Value))
            {
                hexCount++;
                hexString.Append(nextNextChar.Value);
                reader.Read();
            }
            else
            {
                // TODO potentially need to handle whitespace here
                break;
            }

            if (hexCount == 6)
            {
                // TODO potentially need to handle whitespace here
                break;
            }
        }

        var hex = Convert.ToInt32(hexString.ToString(), 16);

        if (hex == 0 || hex > 0x10FFFF || char.IsSurrogate((char)hex))
        {
            return '\uFFFD';
        }

        return (char)hex;
    }

    static bool IsIdentCodePoint(char c)
    {
        if (IsIdentStartCodePoint(c)) return true;
        if (char.IsAsciiDigit(c)) return true;
        return c == '-';
    }

    static bool IsIdentStartCodePoint(char c)
    {
        if (char.IsAsciiLetter(c)) return true;
        if (IsNonAsciiIdentCodePoint(c)) return true;
        return c == '_';
    }

    static bool StartsAnIdentSequence(char a, char? b, char? c)
    {
        if (IsIdentStartCodePoint(a)) return true;
        if (!b.HasValue) return false;
        if (AreTwoCodePointsAValidEscape(a, b.Value)) return true;
        if (a != '-') return false;

        if (IsIdentStartCodePoint(b.Value) || b.Value == '-') return true;
        return c.HasValue && AreTwoCodePointsAValidEscape(b.Value, c.Value);
    }

    static bool StartsANumber(char a, char? b, char? c)
    {
        if (char.IsAsciiDigit(a)) return true;
        if (!b.HasValue) return false;
        return a switch
        {
            '+' or '-' or '.' when char.IsAsciiDigit(b.Value) => true,
            '+' or '-' when b.Value is '.' => c.HasValue && char.IsAsciiDigit(c.Value),
            _ => false
        };
    }

    static bool IsNonAsciiIdentCodePoint(char c)
    {
        // https://drafts.csswg.org/css-syntax/#non-ascii-ident-code-point
        return (int)c is
            0x00B7 or
            >= 0x00C0 and <= 0x00D6 or
            >= 0x00D8 and <= 0x00F6 or
            >= 0x00F8 and <= 0x037D or
            >= 0x037F and <= 0x1FFF or
            0x200C or
            0x200D or
            0x203F or
            0x2040 or
            >= 0x2070 and <= 0x218F or
            >= 0x2C00 and <= 0x2FEF or
            >= 0x3001 and <= 0xD7FF or
            >= 0xF900 and <= 0xFDCF or
            >= 0xFDF0 and <= 0xFFFD or
            >= 0x10000;
    }

    static bool AreTwoCodePointsAValidEscape(char first, char? second)
    {
        if (first != '\\') return false;
        return second.HasValue && second != '\n';
    }

    public void Dispose()
    {
        CssStream.Dispose();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return CssStream.DisposeAsync();
    }
}