using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Railgun.Api;
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
                List<object> l => "[" + string.Join(" ", l.Select(Repr)) + "]",
                Dictionary<object, object> l => "{" + string.Join("  ", 
                    l.Select(pair => $"{Repr(pair.Key)} {Repr(pair.Value)}")) + "}",
                QuoteExpr q => $"'{Repr(q.Data)}",
                Keyword k => $":{k.Name}",
                string s => SymbolDisplay.FormatLiteral(s, true),
                null => "null",
                bool b => b ? "true" : "false",
                _ => o.ToString()
            };
        }
    }
}