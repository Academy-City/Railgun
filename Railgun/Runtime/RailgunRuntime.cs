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
                env[IsVariadic] = new SeqExpr(args.Skip(Args.Length).ToImmutableList());
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
            
            NewFn("concat", x => ((SeqExpr) x[0]).Concat((SeqExpr) x[1]));
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
                SeqExpr seq when depth != 0 => seq.Map(x => EvalQuasiquote(x, env, depth)),
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
                SeqExpr seq when seq.Children.Count >= 1 && TryGetMacro(seq[0], env, out var m)
                    && qqDepth == 0 => ExpandMacros(
                    m.Eval(this, seq.Children.Skip(1).ToArray()), env, qqDepth
                ),
                SeqExpr seq => seq.Map(x => ExpandMacros(x, env, qqDepth)),
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
                case SeqExpr seq: // function-like
                    // TODO: keyword check
                    if (seq[0] is NameExpr n)
                    {
                        switch (n.Name)
                        {
                            case "let":
                                return env[((NameExpr) seq[1]).Name] = Eval(seq[2], env);
                            case "set":
                                return env.Set(((NameExpr) seq[1]).Name, Eval(seq[2], env));
                            case "if":
                                var ifCond = (bool) Eval(seq.Children[1], env);
                                if (ifCond) return Eval(seq.Children[2], env);
                                return seq.Children.Count >= 4 ? Eval(seq.Children[3], env) : null;
                            case "do":
                                var nenv = new RailgunEnvironment(env);
                                object doRet = null;
                                foreach (var e in seq.Children.Skip(1))
                                {
                                    doRet = Eval(e, nenv);
                                }
                                return doRet;
                            case "while":
                                var wcond = seq[1];
                                var wbody = seq.Children.Skip(2).ToArray();
                                while ((bool) Eval(wcond, env))
                                {
                                    foreach (var x in wbody)
                                    {
                                        Eval(x, env);
                                    }
                                }
                                return null;
                            case "fn":
                                return new RailgunFn(
                                    env,
                                    ((List<object>) seq[1])
                                    .Select(x => ((NameExpr) x).Name)
                                    .ToArray(),
                                    seq.Children.Skip(2).ToArray()
                                );
                            case "macro":
                                return new RailgunMacro(
                                    env,
                                    ((List<object>) seq.Children[1])
                                    .Select(x => ((NameExpr) x).Name)
                                    .ToArray(),
                                    seq.Children.Skip(2).ToArray());
                        }
                    }
                    var fn = Eval(seq.Children[0], env);
                    switch (fn)
                    {
                        case IRailgunFn lfn:
                            var arr = seq.Children.Skip(1).Select(x => Eval(x, env)).ToArray();
                            return lfn.Eval(this, arr);
                        case IRailgunMacro:
                            throw new RailgunRuntimeException("Macros should not be evaluating this late.");
                        default:
                            throw new RailgunRuntimeException("Not a function");
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