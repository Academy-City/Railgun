using System.Linq;
using Railgun.BytecodeRuntime;
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
                QuasiquoteHelper(s, depth);
        }

        public static Seq QuasiquoteHelper(Seq s, int depth)
        {
            Seq n = Nil.Value;
            foreach (var item in s.Reverse())
            {
                if (item is Cell {Head: NameExpr {Name: "splice"}} c)
                {
                    var cw = ((Cell) c.Tail).Head;
                    n = new Cell(new NameExpr("concat"), new Cell(
                        LowerQuasiquotes(cw, depth - 1), new Cell(n, Nil.Value)));
                    continue;
                }
                n = new Cell(new NameExpr("cons"), new Cell(
                    LowerQuasiquotes(item, depth), new Cell(n, Nil.Value)));
            }
            return n;
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
                case "cfn":
                    var (cfnArgs, cfnBody) = (Cell) c.Tail;
                    var cfn = new CompiledFn(
                        ((Cell) cfnArgs)
                        .Select(n => ((NameExpr) n).Name)
                        .ToArray(),
                        cfnBody);
                    return cfn;
            }
            return c.Map(CompileFunctions);
        }
    }
}