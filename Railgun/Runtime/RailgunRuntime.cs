using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static string LoadEmbeddedFile(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }
        
        private readonly string _workingDirectory;
        public readonly RailgunEnvironment Globals = new();

        private void NewFn(string name, Func<object[], object> body)
        {
            Globals.Let(name, new BuiltinClosure(body));
        }
        
        private void NewMacro(string name, Func<object[], object> body)
        {
            Globals.Let(name, new BuiltinClosure(body, true));
        }
        
        public RailgunRuntime(string workingDirectory = "")
        {
            _workingDirectory = workingDirectory;

            Globals.Let("true", true);
            Globals.Let("false", false);
            Globals.Let("nil", Nil.Value);

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
            NewFn("foreach-fn", x =>
            {
                var list = (IEnumerable<object>) x[0];
                var f = (IRailgunClosure) x[1];
                foreach (var item in list)
                {
                    f.Eval(this, new Cell(item, Nil.Value));
                }
                return null;
            });

            NewFn("macroexpand", x => ExpandMacros(x[0], Globals));
            NewFn("str/fmt", x => string.Format((string) x[0], x.Skip(1).ToArray()));
            NewFn("|>", x => x.Skip(1).Aggregate(x[0], 
                (cx, fn) => ((IRailgunClosure) fn).Eval(this, new Cell(cx, Nil.Value))));
            
            RunProgram(new SweetParser(LoadEmbeddedFile("Railgun.core.core.rgx")).ParseSweetProgram());
        }

        public static object WalkQuasiquote(object ex, Func<object, object> fn, int depth = 0)
        {
            if (ex is not Seq seq) return depth == 0 ? fn(ex) : ex;
            // Seq is not evaluatable (does not have a head that is a NameExpr)
            // in this case, evaluate for its children, if not subquoted
            if (seq is not Cell c || seq.First() is not NameExpr n)
                return depth == 0 ? fn(ex) : seq.Map(x => WalkQuasiquote(x, fn, depth));

            return n.Name switch
            {
                "quasiquote" => WalkQuasiquote(((Cell) c.Tail).Head, fn, depth + 1),
                "unquote" => WalkQuasiquote(((Cell) c.Tail).Head, fn, depth - 1),
                _ => depth == 0 ? fn(ex) : seq.Map(x => WalkQuasiquote(x, fn, depth))
            };

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
                            case "quote":
                                return ((Cell) rest).Head;
                            case "quasiquote":
                                return EvalQuasiquote(seq, env);
                            case "let":
                                var (letVars, _) = rest.TakeN(2);
                                return env.Let(((NameExpr) letVars[0]).Name, Eval(letVars[1], env));
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
                            case "struct":
                            case "fn":
                            case "macro": 
                                throw new RailgunRuntimeException("Unexpected uncompiled function");
                            case "use":
                                var (uArg, _) = (Cell) rest;
                                var uenv = new RailgunEnvironment(env);
                                var path = Path.Join(_workingDirectory, (string) uArg);
                                var uProgram = ProgramLoader.LoadProgram(path);
                                RunProgram(uProgram, uenv);
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

                    return lfn.Eval(this, seq.Tail.Map(x => Eval(x, env)));
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