using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Railgun.Grammar;
using Railgun.Types;

namespace Railgun.Runtime
{
    public class RailgunRuntimeException : Exception
    {
        public RailgunRuntimeException(string message) : base(message)
        {
            
        }
    }
    
    public interface IRailgunFn
    {
        public object Eval(RailgunRuntime runtime, Seq args);
        public bool IsMacro { get; }
    }
    
    public class RailgunFn : IRailgunFn {
        private readonly IEnvironment _env;
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public Seq Body { get; }

        public bool IsMacro { get; }
        
        public RailgunFn(IEnvironment env, string[] args, Seq body, bool isMacro = false)
        {
            _env = env;
            // TODO: Better checks
            Args = args;
            IsMacro = isMacro;

            if (args.Length >= 2 && args[^2] == "&")
            {
                IsVariadic = args[^1];
                Args = Args.SkipLast(2).ToArray();
            }
            Body = body;
        }
        
        private void SetupArgs(Seq args, IEnvironment env)
        {
            var ac = args.Count();
            if (ac != Args.Length && IsVariadic != "" && ac < Args.Length)
            {
                throw new RailgunRuntimeException(
                    $"Wrong number of args: Requested {Args.Length}, Got {ac}");
            }

            foreach (var argName in Args)
            {
                var (argValue, tail) = (Cell) args;
                env[argName] = argValue;
                args = tail;
            }

            if (IsVariadic != "")
            {
                env[IsVariadic] = Seq.Create(args);
            }
        }
        
        public object Eval(RailgunRuntime runtime, Seq args)
        {
            var env = new RailgunEnvironment(_env);
            SetupArgs(args, env);
            
            var next = Body;
            while (next is Cell c)
            {
                var ev = runtime.Eval(c.Head, env);
                if (c.Tail is Nil)
                {
                    return ev;
                }
                next = c.Tail;
            }
            return null;
        }
    }

    public sealed class BuiltinFn : IRailgunFn
    {
        public Func<object[], object> Body { get; }
        public bool IsMacro { get; }

        public BuiltinFn(Func<object[], object> body, bool isMacro = false)
        {
            Body = body;
            IsMacro = isMacro;
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return Body(args.ToArray());
        }
    }

    public class RailgunRuntime
    {
        private readonly string _workingDirectory;
        public readonly RailgunEnvironment Globals = new();

        private void NewFn(string name, Func<object[], object> body)
        {
            Globals[name] = new BuiltinFn(body);
        }
        
        private void NewMacro(string name, Func<object[], object> body)
        {
            Globals[name] = new BuiltinFn(body, true);
        }
        
        public RailgunRuntime(string workingDirectory = "")
        {
            _workingDirectory = workingDirectory;

            Globals["true"] = true;
            Globals["false"] = false;
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
            NewFn("macroexpand", x => ExpandMacros(x[0], Globals));
            NewFn("str/fmt", x => string.Format((string) x[0], x.Skip(1).ToArray()));
            NewFn("|>", x => x.Skip(1).Aggregate(x[0], 
                (cx, fn) => ((IRailgunFn) fn).Eval(this, new Cell(cx, Nil.Value))));
            
            RunProgram(new Parser(Prelude).ParseProgram());
        }
        private const string Prelude = @"
(let let-macro (macro [name args & body]
    `(let ,name ,(concat `(macro ,args) body))))

(let-macro let-fn [name args & body]
    `(let ,name ,(concat `(fn ,args) body)))
";

        private static object WalkQuasiquote(object ex, Func<object, object> fn, int depth = 0)
        {
            return ex switch
            {
                QuoteExpr {IsQuasiquote: true} qex => WalkQuasiquote(qex.Value, fn, depth + 1),
                UnquoteExpr uex => WalkQuasiquote(uex.Value, fn, depth - 1),
                Seq seq when depth != 0 => seq.Map(x => WalkQuasiquote(x, fn, depth)),
                _ => depth == 0 ? fn(ex) : ex
            };
        }
        
        // evaluates unquoted values inside quasiquotes
        // eval is only allowed at depth 0
        private object EvalQuasiquote(object ex, IEnvironment env)
        {
            return WalkQuasiquote(ex, x => Eval(x, env));
        }

        private static bool TryGetMacro(object ex, IEnvironment env, out IRailgunFn mac)
        {
            mac = null;
            switch (ex)
            {
                case IRailgunFn{IsMacro: true} m:
                    mac = m;
                    return true;
                case NameExpr nex:
                {
                    var x = env[nex.Name];
                    if (x is IRailgunFn{IsMacro: true} m2)
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

        public object Eval(object ex, IEnvironment env = null, bool topLevel = false)
        {
            env ??= Globals;
            if (topLevel)
            {
                ex = ExpandMacros(ex, env);
            }

            switch (ex)
            {
                case NameExpr nex:
                    return env[nex.Name];
                case QuoteExpr q:
                    return q.IsQuasiquote ? EvalQuasiquote(q, env) : q.Value;
                case UnquoteExpr:
                    throw new RailgunRuntimeException("Unquote is not allowed outside of quasiquotes");
                case Cell seq: // function-like
                    // TODO: keyword check
                    if (seq.Head is NameExpr n)
                    {
                        var rest = seq.Tail;
                        switch (n.Name)
                        {
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
                                var (fnArgs, fnBody) = (Cell) rest;
                                return new RailgunFn(
                                    env,
                                    ((List<object>) fnArgs)
                                    .Select(x => ((NameExpr) x).Name)
                                    .ToArray(),
                                    fnBody,
                                    n.Name == "macro"
                                );
                            // TODO: make asynchronous
                            case "use":
                                var (uArg, _) = (Cell) rest;
                                var path = Path.Join(_workingDirectory, (string) uArg+".rg");
                                var uenv = new RailgunEnvironment(env);
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
                    switch (fn)
                    {
                        case IRailgunFn lfn:
                            if (lfn.IsMacro)
                            {
                                throw new RailgunRuntimeException("Macros should not be evaluating this late.");
                            }
                            var fnArgs = seq.Tail.Map(x => Eval(x, env));
                            return lfn.Eval(this, fnArgs);
                        default:
                            throw new RailgunRuntimeException($"{RailgunLibrary.Repr(fn)} is not a function");
                    }
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