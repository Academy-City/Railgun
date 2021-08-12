using System.Linq;
using Railgun.Types;

namespace Railgun.Runtime
{
    public static class Optimizer
    {
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
            
            var nseq = s.Map(x => LowerQuasiquotes(x, depth));
            // return nseq;
            return depth == 0 ? nseq : new Cell(new NameExpr("seq"), nseq);
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