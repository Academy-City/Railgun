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
            if (Pos >= Source.Length) return 0;
            while (true)
            {
                switch (Current)
                {
                    case '#':
                        Pos++;
                        while (Current != '\n')
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
                list.Add(new Token(TokenType.Newline, ""));
            }
            else if (_indentStack.Peek() < wsCount)
            {
                list.Add(new Token(TokenType.Indent, ""));
                _indentStack.Push(wsCount);
            }

            while (_indentStack.Peek() > wsCount)
            {
                list.Add(new Token(TokenType.Dedent, ""));
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
                        '(' => new Token(TokenType.LParen, ""),
                        ')' => new Token(TokenType.RParen, ""),
                        '[' => new Token(TokenType.LBracket, ""),
                        ']' => new Token(TokenType.RBracket, ""),
                        '\'' => new Token(TokenType.Quote, ""),
                        '`' => new Token(TokenType.Quasiquote, ""), 
                        ',' => new Token(TokenType.Unquote, ""),
                        
                        _ => throw new Exception("unexpected token")
                    });
                    Pos++;
                }
                else
                {
                    throw new Exception("unexpected token");
                }
            }

            while (_indentStack.Pop() != 0)
            {
                list.Add(new Token(TokenType.Dedent, ""));
            }
            
            list.Add(new Token(TokenType.Eof, ""));
            return list;
        }
    }
}