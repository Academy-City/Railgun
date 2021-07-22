using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Railgun.Grammar
{
    [JsonConverter(typeof(StringEnumConverter))]
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
        Unquote,
        // for sweet lexer
        Indent,
        Dedent,
        Newline,
        Eof
    }
    
    public record Token(TokenType Kind, string Value);

    public abstract class BaseLexer
    {
        protected int Pos;
        protected string Source;
        protected char Current => Source[Pos];
        protected bool Eof => Pos >= Source.Length;
        
        protected char Next()
        {
            var c = Current;
            Pos++;
            return c;
        }
        
        protected void MustBe(char c)
        {
            if (Next() != c)
            {
                throw new Exception("Must be char");
            }
        }
        
        protected Token String()
        {
            MustBe('"');
            var d = "";
            while (Current != '"')
            {
                if (Current == '\\')
                {
                    Pos++;
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
        
        protected static bool IsSymbol(char c, bool start = false)
        {
            if (start && char.IsNumber(c)) return false;

            return char.IsLetterOrDigit(c) || "=+-*/!?_<|>&.".Contains(c);
        }

        protected Token Numeric()
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

        protected Token Name()
        {
            var v = "";
            while (!Eof && IsSymbol(Current))
            {
                v += Next();
            }
            return new Token(TokenType.NameSymbol, v);
        }
    }
    
    public class Lexer : BaseLexer
    {
        public Lexer(string source)
        {
            Source = source;
        }
        
        public List<Token> Lex()
        {
            var list = new List<Token>();
            while (Pos < Source.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    Pos++;
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
            list.Add(new Token(TokenType.Eof, ""));
            return list;
        }
    }
}