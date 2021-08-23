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
            while (true)
            {
                if (ex is not Seq s) return depth == 0 ? ex : new QuoteExpr(ex).Lower();
                if (s is Cell {Head: NameExpr name} c)
                {
                    switch (name.Name)
                    {
                        case "quasiquote":
                            ex = ((Cell) c.Tail).Head;
                            depth++;
                            continue;
                        case "unquote":
                            ex = ((Cell) c.Tail).Head;
                            depth--;
                            continue;
                    }
                }

                return depth == 0 ? s : QuasiquoteHelper(s, depth);
            }
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
    }
}