using System;
using System.Collections.Generic;

namespace Railgun.Grammar
{
    public enum TokenType
    {
        LParen,
        RParen,
        LBracket,
        RBracket,
        
        NameSymbol,
        Numeric,
        String,
        
        Quote,
        Quasiquote,
        Unquote
    }
    
    public record Token(TokenType Kind, string Value);
    
    public class Lexer
    {
        private int _pos;
        private readonly string _source;
        private char Current => _source[_pos];
        private bool Eof => _pos >= _source.Length;

        public Lexer(string source)
        {
            _source = source;
        }
        
        public List<Token> Lex()
        {
            var list = new List<Token>();
            while (_pos < _source.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    _pos++;
                }
                else if (Current == ';') // comments
                {
                    _pos++;
                    while (_pos < _source.Length && Current != '\n')
                    {
                        _pos++;
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
                    _pos++;
                }
                else
                {
                    throw new Exception("unexpected token");
                }
            }
            return list;
        }

        public char Next()
        {
            var c = Current;
            _pos++;
            return c;
        }

        private static bool IsSymbol(char c, bool start = false)
        {
            if (start && char.IsNumber(c)) return false;

            return char.IsLetterOrDigit(c) || "=+-*/!?_<|>&".Contains(c);
        }

        private void MustBe(char c)
        {
            if (Next() != c)
            {
                throw new Exception("Must be char");
            }
        }

        private Token String()
        {
            MustBe('"');
            var d = "";
            while (Current != '"')
            {
                if (Current == '\\')
                {
                    _pos++;
                    d += Next() switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        _ => throw new Exception("Unexpected Escape")
                    };
                }
                else
                {
                    d += Next();
                }
            }
            MustBe('"');
            return new Token(TokenType.String, d);
        }

        private Token Numeric()
        {
            var v = "";
            while (!Eof && char.IsNumber(Current))
            {
                v += Next();
            }

            if (!Eof && Current == '.')
            {
                v += Next();
                while (!Eof && char.IsNumber(Current))
                {
                    v += Next();
                }
            }
            return new Token(TokenType.Numeric, v);
        }

        private Token Name()
        {
            var v = "";
            while (!Eof && IsSymbol(Current))
            {
                v += Next();
            }
            return new Token(TokenType.NameSymbol, v);
        }
    }
}