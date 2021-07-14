using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public SeqExpr ParseSequence()
        {
            return new(ParseCollection(TokenType.LParen, TokenType.RParen).ToImmutableList());
        }
    }
}