using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Railgun.Grammar
{
    public record LinePosition(int Line, int Column);
    
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
    
    public record Token(TokenType Kind, string Value, int Position);

    public abstract class BaseLexer
    {
        protected int Pos;
        protected string Source;
        protected char Current => Source[Pos];
        protected bool Eof => Pos >= Source.Length;

        public static LinePosition CalculatePosition(string src, int position)
        {
            var row = 1;
            var lastN = 0;
            for (var i = 0; i < position; i++)
            {
                var c = src[i];
                if (c == '\n')
                {
                    lastN = i;
                    row++;
                }
            }
            return new LinePosition(row, position - lastN + row);
        }
        
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
                throw new ParseException($"Expected \'{c}\'", Pos);
            }
        }
        
        protected Token String()
        {
            var startingPos = Pos;
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
                        _ => throw new ParseException("Unexpected Escape", Pos)
                    };
                }
                else
                {
                    d += Next();
                }
            }
            MustBe('"');
            return new Token(TokenType.String, d, startingPos);
        }
        
        protected static bool IsSymbol(char c, bool start = false)
        {
            if (start && char.IsNumber(c)) return false;

            return char.IsLetterOrDigit(c) || "=+-*/!?_<|>&.".Contains(c);
        }

        protected Token Numeric()
        {
            var startingPos = Pos;
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
            return new Token(TokenType.Numeric, v, startingPos);
        }

        protected Token Name()
        {
            var startingPos = Pos;
            var name = "";
            while (!Eof && IsSymbol(Current))
            {
                name += Next();
            }
            return new Token(TokenType.NameSymbol, name, startingPos);
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
            list.Add(new Token(TokenType.Eof, "", Pos));
            return list;
        }
    }
}