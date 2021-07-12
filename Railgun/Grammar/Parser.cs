using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
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
                    return ParseList();
                case TokenType.RParen:
                    throw new NotImplementedException();
                case TokenType.Quote:
                    _pos++;
                    return new QuoteExpr(ParseExpr());
                case TokenType.Quasiquote:
                    _pos++;
                    return new QuoteExpr(ParseExpr(), true);
                case TokenType.Unquote:
                    _pos++;
                    return new UnquoteExpr(ParseExpr());
                case TokenType.NameSymbol:
                    return new NameExpr(Next().Value);
                case TokenType.Numeric:
                    return int.Parse(Next().Value);
                case TokenType.String:
                    return Next().Value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SeqExpr ParseList()
        {
            var list = new List<object>();
            MustBe(TokenType.LParen);
            while (Current.Kind != TokenType.RParen)
            {
                list.Add(ParseExpr());
            }
            MustBe(TokenType.RParen);
            return new SeqExpr(list.ToImmutableList());
        }
    }
}