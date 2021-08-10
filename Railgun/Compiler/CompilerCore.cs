using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Railgun.Grammar;
using Railgun.Grammar.Sweet;
using Railgun.Runtime;
using Railgun.Types;

namespace Railgun.Compiler
{
    public record Macro(Delegate Fn);

    public class CompilerCore
    {
        public Root Globals { get; }
        public delegate dynamic RgFn(dynamic[] d);

        public delegate object SequenceT(params object[] d);

        public CompilerCore()
        {
            Globals = new Root(new Dictionary<string, object>
            {
                ["true"] = true,
                ["false"] = false,
                ["num"] = 42,
                ["+"] = (RgFn) (xs => xs.Aggregate((a, b) => a + b)),
                ["-"] = (RgFn) (xs => xs[0] - xs[1]),
                ["*"] = (RgFn) (xs => xs.Aggregate((a, b) => a * b)),
                ["/"] = (RgFn) (xs => xs.Aggregate((a, b) => a / b)),
                ["="] = (RgFn) (xs => xs[0].Equals(xs[1])),
                ["!="] = (RgFn) (xs => !xs[0].Equals(xs[1])),
                ["<"] = (RgFn) (xs => xs[0] < xs[1]),
                ["<="] = (RgFn) (xs => xs[0] <= xs[1]),
                [">"] = (RgFn) (xs => xs[0] > xs[1]),
                [">="] = (RgFn) (xs => xs[0] >= xs[1]),


                ["seq"] = (SequenceT) Seq.Create,
                ["concat"] = (Func<IEnumerable<object>, IEnumerable<object>, Seq>)
                    ((a, b) =>
                    {
                        return Seq.Create(a.Concat(b));
                    }),
                ["print"] = (Func<object, object>) (d =>
                {
                    Console.WriteLine(d);
                    return null;
                })
            });
        }
        
        public static Expression CreateIndexSet(Expression collection, Expression indexer, Expression value)
        {
            var binder = Binder.SetIndex(
                CSharpBinderFlags.None, typeof(object),
                new[] { 
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), 
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                });
            return Expression.Dynamic(binder, typeof(object), collection, indexer, value);
        }
        
        public static Expression CreateBinOp(ExpressionType op, Expression left, Expression right)
        {
            var binder = Binder.BinaryOperation(
                CSharpBinderFlags.None, op, typeof(object),
                new[] { 
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), 
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                });
            return Expression.Dynamic(binder, typeof(object), left, right);
        }

        public static Expression CreateDelegateCall(params Expression[] args)
        {
            var arr = new CSharpArgumentInfo[args.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = CSharpArgumentInfo.Create(default, null);
            }

            var binder = Binder.Invoke(CSharpBinderFlags.None, typeof(object), arr);
            return Expression.Dynamic(binder, typeof(object), args);
        }

        public static Dictionary<string, ParameterExpression> FindDecl(Seq seq)
        {
            var dict = new Dictionary<string, ParameterExpression>();
            foreach (var s in seq)
            {
                if (s is Cell {Head: NameExpr { Name: "let" }, Tail: Cell { Head: NameExpr w }})
                {
                    dict[w.Name] = Expression.Parameter(typeof(object), w.Name);
                }
            }
            return dict;

        }

        public static Expression GenerateEval(object expr, ICompiledEnv compiledEnv)
        {
            switch (expr)
            {
                case NameExpr n:
                    return compiledEnv[n.Name];
                case Cell {Head: NameExpr name, Tail: var rest}:
                    switch (name.Name)
                    {
                        case "quote":
                            return Expression.Constant(((Cell) rest).Head);
                        case "let":
                            var (letVars, _) = rest.TakeN(2);
                            return compiledEnv.Let(((NameExpr) letVars[0]).Name, GenerateEval(letVars[1], compiledEnv));
                        case "if":
                            // TODO: make if return
                            var (ifVars, elseTail) = rest.TakeN(2);
                            if (elseTail is not Cell ic)
                            {
                                return BlockBuilder.GenIf(GenerateEval(ifVars[0], compiledEnv),
                                    GenerateEval(ifVars[1], compiledEnv));
                            }
                            return BlockBuilder.GenIf(GenerateEval(ifVars[0], compiledEnv),
                                GenerateEval(ifVars[1], compiledEnv),
                                GenerateEval(ic.Head, compiledEnv));
                        case "do":
                            var bb = new BlockBuilder(compiledEnv, FindDecl(rest));
                            bb.AddExprs(rest.Select(x => GenerateEval(x, bb)));
                            return bb.Build();
                        case "while":
                            var (wcond, wbody) = (Cell) rest;

                            var wbb = new BlockBuilder(compiledEnv, FindDecl(wbody));
                            wbb.AddExprs(wbody.Select(x => GenerateEval(x, wbb)));
                            
                            return BlockBuilder.GenWhile(
                                GenerateEval(wcond, compiledEnv),
                                wbb.Build());
                        case "fn":
                            var (fnArgs, fnBody) = (Cell) rest;
                            var fnb = new BlockBuilder(compiledEnv, FindDecl(fnBody),
                                ((Seq) fnArgs).Select(x => ((NameExpr) x).Name));
                            fnb.AddExprs(fnBody.Select(x => GenerateEval(x, fnb)));
                            return fnb.BuildFunction();
                        case "macro":
                            var (macArgs, macBody) = (Cell) rest;
                            var mcb = new BlockBuilder(compiledEnv, FindDecl(macBody),
                                ((Seq) macArgs).Select(x => ((NameExpr) x).Name));
                            mcb.AddExprs(macBody.Select(x => GenerateEval(x, mcb)));
                            return Expression.New(typeof(Macro).GetConstructors().First(), 
                                mcb.BuildFunction());
                    }
                    break;
            }

            if (expr is Cell c)
            {
                var fn = GenerateEval(c.Head, compiledEnv);
                var args = c.Tail.Select(x => GenerateEval(x, compiledEnv));
                return BlockBuilder.GenCall(fn, args.ToArray());
            }

            return Expression.Constant(expr);
        }
        
        public object DesugarQuasiquotes(object ex, int depth = 0)
        {
            if (ex is Seq s)
            {
                if (s is Cell {Head: NameExpr name} c)
                {
                    switch (name.Name)
                    {
                        case "quasiquote":
                            return DesugarQuasiquotes(((Cell) c.Tail).Head, depth + 1);
                        case "unquote":
                            return DesugarQuasiquotes(((Cell) c.Tail).Head, depth - 1);
                    }
                }

                var nseq = s.Map(x => DesugarQuasiquotes(x, depth));

                if (depth == 0)
                {
                    return nseq;
                }

                return new Cell(new NameExpr("seq"), nseq);
            }
            return ex;
        }

        public object ExpandMacros(object ex)
        {
            if (ex is Cell {Head: NameExpr {Name: var name}} c)
            {
                if (Globals.Dict.TryGetValue(name, out var m) && m is Macro mac)
                {
                    // dynamic stuff
                    if (mac.Fn.GetType().Name.StartsWith("VarFunc`"))
                    {
                        var l = mac.Fn.Method.GetParameters().Length - 2;
                        var re = c.Tail.Take(l).Append(c.Tail.Skip(l).ToArray()).ToArray();
                        try
                        {
                            return ExpandMacros(mac.Fn.DynamicInvoke(re));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw new NotImplementedException();
                        }
                    }

                    return ExpandMacros(mac.Fn.DynamicInvoke(c.Tail.ToArray()));
                }

                return c.Map(ExpandMacros);
            }

            return ex;
        }

        public void CompileSource(IEnumerable<object> exprs)
        {
            foreach (var expr in exprs)
            {
                var exprn = DesugarQuasiquotes(expr);
                exprn = ExpandMacros(exprn);
                CompileExpr(exprn);
            }
        }

        public object CompileExpr(object expr)
        {
            var gen = GenerateEval(expr, Globals);
            return Globals.WrapAndRun(gen);
        }
        
        public void DoStuff()
        {
            var ss = new Parser(@"
(let let-macro
    (macro (name args &body) 
        `(let ,name 1)
    )
)

(let-macro ayaya (x) 1)
").ParseProgram();
            CompileSource(ss);
        }
    }
}