using System.Collections.Generic;
using Railgun.Types;

namespace Railgun.Grammar.Sweet
{
    public class SweetParser : Parser
    {
        public SweetParser(string source) : base(
            new SweetLexer(source).Lex()
        )
        {
            Source = source;
        }

        public List<object> ParseIndented()
        {
            MustBe(TokenType.Indent);

            var l = new List<object>();
            while (Current.Kind != TokenType.Dedent)
            {
                l.Add(ParseSweet());
            }
            
            MustBe(TokenType.Dedent);
            return l;
        }

        public object[] ParseSweetProgram()
        {
            List<object> objs = new();
            while (true)
            {
                while (Current.Kind == TokenType.Newline)
                {
                    Pos++;
                }
                if (Current.Kind == TokenType.Eof) return objs.ToArray();
                objs.Add(ParseSweet());
            }
        }
        
        public object ParseSweet() {
            while (Current.Kind == TokenType.Newline)
            {
                Pos++;
            }
            var fl = new List<object> { ParseExpr() };
            
            // parse single line
            var d = true;
            while (d)
            {
                switch (Current.Kind)
                {
                    case TokenType.Indent:
                    case TokenType.Dedent:
                    case TokenType.Newline:
                    case TokenType.Eof:
                        d = false;
                        break;
                    default:
                        fl.Add(ParseExpr());
                        break;
                }
            }
            
            if (Current.Kind == TokenType.Indent)
            {
                // Console.WriteLine(JsonConvert.SerializeObject(Tokens));
                // Console.WriteLine(Current);
                fl.AddRange(ParseIndented());
            }
            return fl.Count == 1 ? fl[0] : Seq.Create(fl);
        }
    }
}