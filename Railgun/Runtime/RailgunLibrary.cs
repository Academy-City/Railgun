using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Railgun.Types;

namespace Railgun.Runtime
{
    public static class RailgunLibrary
    {
        public static string Repr(object o)
        {
            return o switch
            {
                Seq s => "(" + string.Join(" ", s.Select(Repr)) + ")",
                QuoteExpr q => (q.IsQuasiquote ? "`" : "'") + Repr(q.Value),
                UnquoteExpr uq => "," + Repr(uq.Value),
                List<object> l => "[" + string.Join(" ", l.Select(Repr)) + "]",
                string s => SymbolDisplay.FormatLiteral(s, true),
                _ => o.ToString()
            };
        }
    }
}