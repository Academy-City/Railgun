using System;
using System.Collections.Generic;

namespace Railgun.Grammar.Sweet
{
    // Ignore blank (just whitespace and comment lines)
    // if the next line is INDENT: convert the upper expression tree into 
    public class SweetLexer : BaseLexer
    {
        private readonly Stack<int> _indentStack = new(new []{ 0 });

        public SweetLexer(string source)
        {
            Source = source.Replace(Environment.NewLine, "\n");
        }

        private int TakeWhitespaces()
        {
            var n = Pos;
            while (true)
            {
                if (Pos >= Source.Length) return Pos - n;

                switch (Current)
                {
                    case '#':
                        Pos++;
                        while (Pos < Source.Length && Current != '\n')
                        {
                            Pos++;
                        }
                        break;
                    case '\t':
                    case ' ':
                        Pos++;
                        break;
                    default:
                        return Pos - n;
                }
                
            }
        }

        public void TokenizeWhitespace(List<Token> list)
        {
            var wsCount = TakeWhitespaces();
            while (!Eof && Current == '\n')
            {
                Pos++;
                wsCount = TakeWhitespaces();
            }
            if (_indentStack.Peek() == wsCount)
            {
                list.Add(new Token(TokenType.Newline, "", Pos));
            }
            else if (_indentStack.Peek() < wsCount)
            {
                list.Add(new Token(TokenType.Indent, "", Pos));
                _indentStack.Push(wsCount);
            }

            while (_indentStack.Peek() > wsCount)
            {
                list.Add(new Token(TokenType.Dedent, "", Pos));
                _indentStack.Pop();
            }
        }
        
        public List<Token> Lex()
        {
            var list = new List<Token>();
            TokenizeWhitespace(list);

            while (Pos < Source.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    var n = Next();
                    if (n == '\n')
                    {
                        TokenizeWhitespace(list);
                    }
                }
                else if (Current == '#') // comments
                {
                    Pos++;
                    while (Pos < Source.Length && Current != '\n')
                    {
                        Pos++;
                    }
                }
                else if (Current == '"')
                {
                    list.Add(String());
                }
                else if (char.IsNumber(Current))
                {
                    list.Add(Numeric());
                }
                else if (IsSymbol(Current))
                {
                    list.Add(Name());
                }
                else if ("()[]'`,".Contains(Current))
                {
                    list.Add(Current switch
                    {
                        '(' => new Token(TokenType.LParen, "(", Pos),
                        ')' => new Token(TokenType.RParen, ")", Pos),
                        '[' => new Token(TokenType.LBracket, "[", Pos),
                        ']' => new Token(TokenType.RBracket, "]", Pos),
                        '\'' => new Token(TokenType.Quote, "\\", Pos),
                        '`' => new Token(TokenType.Quasiquote, "`", Pos), 
                        ',' => new Token(TokenType.Unquote, ",", Pos),
                        
                        _ => throw new ParseException("Unexpected token", Pos)
                    });
                    Pos++;
                }
                else
                {
                    throw new ParseException("Unexpected token", Pos);
                }
            }

            while (_indentStack.Pop() != 0)
            {
                list.Add(new Token(TokenType.Dedent, "", Pos));
            }
            
            list.Add(new Token(TokenType.Eof, "", Pos));
            return list;
        }
    }
}