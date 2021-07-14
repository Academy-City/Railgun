using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    
    public abstract class AbstractFunction : IRailgunFnLike {
        private readonly IEnvironment _env;
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public object[] Body { get; }
        
        public AbstractFunction(IEnvironment env, string[] args, object[] body)
        {
            _env = env;
            // TODO: Better checks
            Args = args;

            if (args.Length >= 2 && args[^2] == "&")
            {
                IsVariadic = args[^1];
                Args = Args.SkipLast(2).ToArray();
            }
            Body = body;
        }
        
        protected void SetupArgs(object[] args, IEnvironment env)
        {
            if (args.Length != Args.Length && IsVariadic != "" && args.Length < Args.Length)
            {
                throw new RailgunRuntimeException(
                    $"Wrong number of args: Requested {Args.Length}, Got {args.Length}");
            }
            for (var i = 0; i < Args.Length; i++)
            {
                env[Args[i]] = args[i];
            }

            if (IsVariadic != "")
            {
                env[IsVariadic] = Seq.Create(args.Skip(Args.Length));
            }
        }
        
        public object Eval(RailgunRuntime runtime, object[] args)
        {
            var env = new RailgunEnvironment(_env);
            SetupArgs(args, env);
            
            object r = null;
            foreach (var expr in Body)
            {
                r = runtime.Eval(expr, env);
            }
            return r;
        }
    }

    public interface IRailgunFnLike
    {
        public object Eval(RailgunRuntime runtime, object[] args);
    }

    public interface IRailgunFn : IRailgunFnLike
    { }

    public interface IRailgunMacro : IRailgunFnLike
    { }
    
    public class RailgunFn : AbstractFunction, IRailgunFn
    {
        public RailgunFn(IEnvironment env, string[] args, object[] body) : base(env, args, body) {}
    }
    
    public class RailgunMacro: AbstractFunction, IRailgunMacro
    {
        public RailgunMacro(IEnvironment env, string[] args, object[] body) : base(env, args, body) {}
    }

    public sealed class BuiltinFn : IRailgunFn
    {
        public Func<object[], object> Body { get; }

        public BuiltinFn(Func<object[], object> body)
        {
            Body = body;
        }

        public object Eval(RailgunRuntime runtime, object[] args)
        {
            return Body(args);
        }
    }

    public class RailgunRuntime
    {
        public readonly RailgunEnvironment Globals = new();

        public void NewFn(string name, Func<object[], object> body)
        {
            Globals[name] = new BuiltinFn(body);
        }
        
        public RailgunRuntime()
        {
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
            NewFn("debugmac", x => ExpandMacros(x[0], Globals));
            NewFn("str/fmt", x => string.Format((string) x[0], x.Skip(1).ToArray()));
            NewFn("|>", x =>
            {
                return x.Skip(1)
                    .Aggregate(x[0], 
                        (current, fn) => ((IRailgunFn) fn)
                            .Eval(this, new[] {current}));
            });
            RunProgram(new Parser(Prelude).ParseProgram());
        }
        private const string Prelude = @"
(let let-macro (macro [name args & body]
    `(let ,name ,(concat `(macro ,args) body))))

(let-macro let-fn [name args & body]
    `(let ,name ,(concat `(fn ,args) body)))
";

        // evaluates unquoted values inside quasiquotes
        // eval is only allowed at depth 0
        private object EvalQuasiquote(object ex, IEnvironment env, int depth = 0)
        {
            return ex switch
            {
                QuoteExpr {IsQuasiquote: true} qex => EvalQuasiquote(qex.Value, env, depth + 1),
                UnquoteExpr uex => EvalQuasiquote(uex.Value, env, depth - 1),
                Seq seq when depth != 0 => seq.Map(x => EvalQuasiquote(x, env, depth)),
                _ => depth == 0 ? Eval(ex, env) : ex
            };
        }

        private static bool TryGetMacro(object ex, IEnvironment env, out RailgunMacro mac)
        {
            mac = null;
            switch (ex)
            {
                case RailgunMacro m:
                    mac = m;
                    return true;
                case NameExpr nex:
                {
                    var x = env[nex.Name];
                    if (x is RailgunMacro m2)
                    {
                        mac = m2;
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        public object ExpandMacros(object ex, IEnvironment env, int qqDepth = 0)
        {
            // no eval, just replace macros as you see, except when in qqDepth
            return ex switch
            {
                QuoteExpr {IsQuasiquote: true} qex => 
                    new QuoteExpr(ExpandMacros(qex.Value, env, qqDepth + 1), true),
                UnquoteExpr uex => new UnquoteExpr(ExpandMacros(uex.Value, env, qqDepth - 1)),
                // after the first expansion, recursively expand
                Cell c when TryGetMacro(c.Head, env, out var m)
                    && qqDepth == 0 => ExpandMacros(
                    m.Eval(this, c.Tail.ToArray()), env, qqDepth
                ),
                Seq seq => seq.Map(x => ExpandMacros(x, env, qqDepth)),
                _ => ex
            };
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
                        var r = seq.Tail.ToList();
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
                                var (wcond, wbody) = rest.TakeN(1);
                                while ((bool) Eval(wcond[0], env))
                                {
                                    foreach (var x in wbody)
                                    {
                                        Eval(x, env);
                                    }
                                }
                                return null;
                            case "fn":
                                var (fnArgs, fnBody) = rest.TakeN(1);
                                return new RailgunFn(
                                    env,
                                    ((List<object>) fnArgs[0])
                                    .Select(x => ((NameExpr) x).Name)
                                    .ToArray(),
                                    fnBody.ToArray()
                                );
                            case "macro":
                                var (macArgs, macBody) = rest.TakeN(1);
                                return new RailgunMacro(
                                    env,
                                    ((List<object>) macArgs[0])
                                    .Select(x => ((NameExpr) x).Name)
                                    .ToArray(),
                                    macBody.ToArray());
                        }
                    }
                    var fn = Eval(seq.Head, env);
                    switch (fn)
                    {
                        case IRailgunFn lfn:
                            var fnArgs = seq.Tail.Select(x => Eval(x, env)).ToArray();
                            return lfn.Eval(this, fnArgs);
                        case IRailgunMacro:
                            throw new RailgunRuntimeException("Macros should not be evaluating this late.");
                        default:
                            // Console.WriteLine(fn);
                            Console.WriteLine(fn.GetType().Name);
                            throw new RailgunRuntimeException($"{RailgunLibrary.Repr(fn)} is not a function");
                    }
                default:
                    return ex;
            }
        }
        
        public void RunProgram(IEnumerable<object> program)
        {
            foreach (var expr in program)
            {
                Eval(expr, topLevel: true);
            }
        }
    }
}