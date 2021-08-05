using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Railgun.Grammar;
using Railgun.Grammar.Sweet;
using Railgun.Types;

namespace Railgun.Runtime
{
    public record RailgunExternConstructor(Type Type) : IRailgunClosure
    {
        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return Activator.CreateInstance(Type, args.ToArray());
        }

        public bool IsMacro { get; } = false;
    }
    
    public class RailgunRuntimeException : Exception
    {
        public RailgunRuntimeException(string message) : base(message)
        {
            
        }
    }

    public class RailgunRuntime
    {
        private readonly string _workingDirectory;
        public readonly RailgunEnvironment Globals = new();

        private void NewFn(string name, Func<object[], object> body)
        {
            Globals[name] = new BuiltinClosure(body);
        }
        
        private void NewMacro(string name, Func<object[], object> body)
        {
            Globals[name] = new BuiltinClosure(body, true);
        }
        
        public RailgunRuntime(string workingDirectory = "")
        {
            _workingDirectory = workingDirectory;

            Globals["true"] = true;
            Globals["false"] = false;
            Globals["nil"] = Nil.Value;

            NewFn("car", x => (x[0] as Cell)?.Head);
            NewFn("cdr", x => (x[0] as Cell)?.Tail);
            NewFn("cons", x => new Cell(x[0], (Seq) x[1]));

            NewFn("=", x => x[0].Equals(x[1]));
            NewFn("and", x => (bool) x[0] && (bool) x[1]);
            NewFn("or", x => (bool) x[0] || (bool) x[1]);

            NewFn("+", x => (dynamic) x[0] + (dynamic) x[1]);
            NewFn("-", x => (dynamic) x[0] - (dynamic) x[1]);
            NewFn("*", x => (dynamic) x[0] * (dynamic) x[1]);
            NewFn("/", x => (dynamic) x[0] / (dynamic) x[1]);
            NewFn("<=", x => (dynamic) x[0] <= (dynamic) x[1]);
            NewFn(">=", x => (dynamic) x[0] >= (dynamic) x[1]);
            NewFn("<", x => (dynamic) x[0] < (dynamic) x[1]);
            NewFn(">", x => (dynamic) x[0] > (dynamic) x[1]);
            
            NewFn("concat", x => Seq.Create(
                ((Seq) x[0]).Concat((Seq) x[1])
            ));
            NewFn("repr", x => RailgunLibrary.Repr(x[0]));
            NewFn("print", x =>
            {
                Console.WriteLine(x[0]);
                return null;
            });
            NewFn("idx", x =>
            {
                var coll = (dynamic) x[0];
                var i = (dynamic) x[1];
                return coll[i];
            });
            
            NewFn("list", x => x.ToList());

            NewFn("macroexpand", x => ExpandMacros(x[0], Globals));
            NewFn("str/fmt", x => string.Format((string) x[0], x.Skip(1).ToArray()));
            NewFn("|>", x => x.Skip(1).Aggregate(x[0], 
                (cx, fn) => ((IRailgunClosure) fn).Eval(this, new Cell(cx, Nil.Value))));
            
            RunProgram(new SweetParser(SweetPrelude).ParseSweetProgram());
        }

        private const string SweetPrelude = @"
let let-macro
    macro (name args & body)
        quasiquote
            let ,name ,(concat `(macro ,args) body)

let-macro let-fn (name args & body)
    quasiquote
        let ,name ,(concat `(fn ,args) body)

let-macro def (category name & body)
    quasiquote
        let ,name ,(concat `(,category) body)

let-macro use-as (var name)
    quasiquote
        let ,var (use ,name)
";

        private static object WalkQuasiquote(object ex, Func<object, object> fn, int depth = 0)
        {
            if (ex is Seq seq)
            {
                if (seq is not Cell c || c.Head is not NameExpr n)
                    return depth != 0 ? seq.Map(x => WalkQuasiquote(x, fn, depth)) : fn(ex);

                return n.Name switch
                {
                    "quasiquote" => WalkQuasiquote(((Cell) c.Tail).Head, fn, depth + 1),
                    "unquote" => WalkQuasiquote(((Cell) c.Tail).Head, fn, depth - 1),
                    _ => depth != 0 ? seq.Map(x => WalkQuasiquote(x, fn, depth)) : fn(ex)
                };
            }

            return depth == 0 ? fn(ex) : ex;
        }
        
        // evaluates unquoted values inside quasiquotes
        // eval is only allowed at depth 0
        private object EvalQuasiquote(object ex, IEnvironment env)
        {
            return WalkQuasiquote(ex, x => Eval(x, env));
        }

        private static bool TryGetMacro(object ex, IEnvironment env, out IRailgunClosure mac)
        {
            mac = null;
            switch (ex)
            {
                case IRailgunClosure{IsMacro: true} m:
                    mac = m;
                    return true;
                case NameExpr nex:
                {
                    var x = env[nex.Name];
                    if (x is IRailgunClosure{IsMacro: true} m2)
                    {
                        mac = m2;
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        private object ExpandMacros(object ex, IEnvironment env)
        {
            return WalkQuasiquote(ex, x =>
            {
                if (x is Cell c && TryGetMacro(c.Head, env, out var mac))
                {
                    // Console.WriteLine("mac expand");
                    // Console.WriteLine(c.Head);
                    return ExpandMacros(mac.Eval(this, c.Tail), env);
                }
                return x;
            });
        }
        
        private static object CompileFunctions(object ex)
        {
            return WalkQuasiquote(ex, x =>
            {
                if (x is not Cell c) return x;
                
                if (c.Head is NameExpr h)
                {
                    switch (h.Name)
                    {
                        case "fn":
                        case "macro":
                            var (fnArgs, fnBody) = (Cell) c.Tail;

                            return new RailgunFn(
                                ((Cell) fnArgs)
                                .Select(n => ((NameExpr) n).Name)
                                .ToArray(),
                                fnBody, h.Name == "macro");
                    }
                }
                return c.Map(CompileFunctions);
            });
        }

        public object Eval(object ex, IEnvironment env = null, bool topLevel = false)
        {
            env ??= Globals;
            if (topLevel)
            {
                ex = ExpandMacros(ex, env);
                ex = CompileFunctions(ex);
            }

            switch (ex)
            {
                case IRailgunFn cm:
                    return cm.BuildClosure(env);
                case NameExpr nex:
                    return env[nex.Name];
                case Cell seq: // function-like
                    if (seq.Head is NameExpr n)
                    {
                        var rest = seq.Tail;
                        switch (n.Name)
                        {
                            case "struct":
                                return new RecordType(rest.Select(s => ((NameExpr) s).Name).ToList());
                            case "quote":
                                return ((Cell) rest).Head;
                            case "quasiquote":
                                return EvalQuasiquote(seq, env);
                            case "let":
                                var (letVars, _) = rest.TakeN(2);
                                return env[((NameExpr) letVars[0]).Name] = Eval(letVars[1], env);
                            case "set":
                                var (setVars, _) = rest.TakeN(2);
                                return env.Set(((NameExpr) setVars[0]).Name, Eval(setVars[1], env));
                            case "if":
                                var (ifVars, elseTail) = rest.TakeN(2);
                                var ifCond = (bool) Eval(ifVars[0], env);
                                if (ifCond) return Eval(ifVars[1], env);
                                return elseTail is Cell ec ? Eval(ec.Head, env) : null;
                            case "do":
                                var nenv = new RailgunEnvironment(env);
                                object doRet = null;
                                foreach (var e in rest)
                                {
                                    doRet = Eval(e, nenv);
                                }
                                return doRet;
                            case "while":
                                var (wcond, wbody) = (Cell) rest;
                                while ((bool) Eval(wcond, env))
                                {
                                    foreach (var x in wbody)
                                    {
                                        Eval(x, env);
                                    }
                                }
                                return null;
                            case "fn":
                            case "macro": 
                                throw new RailgunRuntimeException("Unexpected uncompiled function");
                            // TODO: make asynchronous
                            case "use":
                                var (uArg, _) = (Cell) rest;
                                var uenv = new RailgunEnvironment(env);
                                var path = Path.Join(_workingDirectory, (string) uArg+".rg");
                                if (File.Exists(Path.Join(_workingDirectory, (string) uArg + ".rgx")))
                                {
                                    path = Path.Join(_workingDirectory, (string) uArg + ".rgx");
                                    RunProgram(
                                        new SweetParser(File.ReadAllText(path)).ParseSweetProgram(),
                                        uenv
                                    );
                                    return uenv;
                                }
                                RunProgram(
                                    new Parser(File.ReadAllText(path)).ParseProgram(),
                                    uenv
                                );
                                return uenv;
                            case ".":
                                var (dotRoot, dotRest) = (Cell) rest;
                                return dotRest.Aggregate(Eval(dotRoot, env), (current, wx) => 
                                    ((IDottable) current).DotGet(((NameExpr) wx).Name));
                        }
                    }
                    var fn = Eval(seq.Head, env);
                    if (fn is not IRailgunClosure lfn)
                    {
                        throw new RailgunRuntimeException($"{RailgunLibrary.Repr(fn)} is not a function");
                    }
                    if (lfn.IsMacro)
                    {
                        throw new RailgunRuntimeException("Macros should not be evaluating this late.");
                    }

                    var fnArgs = seq.Tail.Map(x => Eval(x, env));
                    return lfn.Eval(this, fnArgs);
                default:
                    return ex;
            }
        }
        
        public void RunProgram(IEnumerable<object> program, IEnvironment env = null)
        {
            env ??= Globals;
            foreach (var expr in program)
            {
                Eval(expr, env, topLevel: true);
            }
        }
    }
}