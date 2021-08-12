using System.Linq;
using Railgun.Types;

namespace Railgun.Runtime
{
    public static class Optimizer
    {
        /// <summary>
        /// Converts a tree of quasiquotes and unquotes into regular quotes.
        /// </summary>
        public static object LowerQuasiquotes(object ex, int depth = 0)
        {
            if (ex is not Seq s) return depth == 0 ? ex : new QuoteExpr(ex).Lower();
            if (s is Cell {Head: NameExpr name} c)
            {
                switch (name.Name)
                {
                    case "quasiquote":
                        return LowerQuasiquotes(((Cell) c.Tail).Head, depth + 1);
                    case "unquote":
                        return LowerQuasiquotes(((Cell) c.Tail).Head, depth - 1);
                }
            }

            return depth == 0 ? s :
                new Cell(new NameExpr("seq"), s.Map(x => LowerQuasiquotes(x, depth)));
        }
        
        public static object CompileFunctions(object ex)
        {
            if (ex is not Cell c) return ex;
            if (c.Head is not NameExpr h) return c.Map(CompileFunctions);
            
            switch (h.Name)
            {
                case "struct":
                    return new StructType(c.Tail.Select(s => ((NameExpr) s).Name).ToList());
                case "fn":
                case "macro":
                    var (fnArgs, fnBody) = (Cell) c.Tail;
                    return new RailgunFn(
                        ((Cell) fnArgs)
                        .Select(n => ((NameExpr) n).Name)
                        .ToArray(),
                        fnBody, h.Name == "macro");
            }
            return c.Map(CompileFunctions);
        }
    }
}