using System;
using System.Collections.Generic;
using System.Linq;
using Railgun.Types;

namespace Railgun.Grammar
{
    public class Parser
    {
        private int _pos = 0;
        private readonly List<Token> _tokens;
        private Token Current => _tokens[_pos];

        public Parser(string source)
        {
            var lexer = new Lexer(source);
            _tokens = lexer.Lex();
        }

        private Token Next()
        {
            var current = Current;
            _pos++;
            return current;
        }

        private void MustBe(TokenType t)
        {
            if (Current.Kind != t)
            {
                throw new Exception($"Expected {t.ToString()}, Got {Current.Kind}");
            }

            _pos++;
        }

        // a program is just a list of exprs
        public object[] ParseProgram()
        {
            List<object> objs = new();
            while (_pos < _tokens.Count)
            {
                objs.Add(ParseExpr());
            }
            return objs.ToArray();
        }

        public object ParseExpr()
        {
            switch (Current.Kind)
            {
                case TokenType.LParen:
                    return ParseSequence();
                case TokenType.LBracket:
                    return ParseList();
                case TokenType.RParen:
                case TokenType.RBracket:
                    throw new NotImplementedException();
                case TokenType.Quote:
                    _pos++;
                    return Seq.Create(new[] { new NameExpr("quote"), ParseExpr() });
                case TokenType.Quasiquote:
                    _pos++;
                    return Seq.Create(new[] { new NameExpr("quasiquote"), ParseExpr() });
                case TokenType.Unquote:
                    _pos++;
                    return Seq.Create(new[] { new NameExpr("unquote"), ParseExpr() });
                case TokenType.NameSymbol:
                    var nv = Next().Value;
                    if (nv.Contains("."))
                    {
                        return new Cell(new NameExpr("."), Seq.Create(nv.Split('.')
                            .Select(x => new NameExpr(x))));
                    }
                    return new NameExpr(nv);
                case TokenType.Numeric:
                    return int.Parse(Next().Value);
                case TokenType.String:
                    return Next().Value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<object> ParseCollection(TokenType left, TokenType right)
        {
            var list = new List<object>();
            MustBe(left);
            while (Current.Kind != right)
            {
                list.Add(ParseExpr());
            }
            MustBe(right);
            return list;
        }

        public List<object> ParseList()
        {
            return ParseCollection(TokenType.LBracket, TokenType.RBracket);
        }

        public Seq ParseSequence()
        {
            return Seq.Create(ParseCollection(TokenType.LParen, TokenType.RParen));
        }
    }
}