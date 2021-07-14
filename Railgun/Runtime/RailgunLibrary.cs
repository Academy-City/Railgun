using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Railgun.Runtime
{
    public static class RailgunLibrary
    {
        public static string Repr(object o)
        {
            return o switch
            {
                List<object> l => "[" + string.Join(" ", l.Select(Repr)) + "]",
                string s => SymbolDisplay.FormatLiteral(s, true),
                _ => o.ToString()
            };
        }
    }
}