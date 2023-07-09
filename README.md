# CSSDotNet
A CSS parser written in C#

## Tokenizer

- [x] Comment Tokens (Fully tokenized despite not being required)
- [x] Numeric Tokens
- [x] Percentage Tokens
- [x] Dimension Tokens
- [x] URL Tokens
- [x] Bad URL Tokens
- [x] Function Tokens
- [x] Ident Tokens
- [x] String Tokens
- [x] Bad String Tokens
- [x] Whitespace Tokens
- [x] Delim Tokens
- [x] At-keyword Tokens
- [x] Hash Tokens (mostly)
- [x] Square, Round, Curly Brackets Tokens
- [x] Semicolon, Colon, Comma tokens
- [x] CDO, CDC Tokens
- [ ] Unicode Range Tokens

## Parser

### Entry Points

- [x] Stylesheet
- [x] Stylesheet's Contents
- [x] Block's Contents
- [x] Rule
- [x] Declaration
- [x] Component Value
- [x] List of Component Values
- [x] CSV of Component Values

### Algorithms

- [x] Stylesheet's Contents
- [x] At Rule
- [x] Qualified Rule
- [x] Block
- [x] Block's Contents
- [x] Declaration
- [x] List of Component Values
- [x] Component Value
- [x] Simple Block
- [x] Function
- [ ] Unicode Ranges
- [ ] An+B Syntax


### Grammar

Needs documenting

### Implemented Specs

- [x] CSS Syntax Level 3 (mostly)

Other specs will be added to this list once they're implemented. For example once `scrollbar-width` and `scrollbar-color` are parsed CSS Scrollbars Level 1 will be added.