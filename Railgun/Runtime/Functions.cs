using System;
using System.Linq;
using Railgun.Types;

namespace Railgun.Runtime
{
    public interface IRailgunClosure
    {
        public object Eval(RailgunRuntime runtime, Seq args);
        public bool IsMacro { get; }
    }
    
    public interface IRailgunFn
    {
        Closure BuildClosure(IEnvironment env);
        object Execute(IEnvironment env, RailgunRuntime runtime, Seq args);
        bool IsMacro { get; }
    }

    public class RailgunFn : IRailgunFn
    {
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public Seq Body { get; }
        public bool IsMacro { get; }
        
        public RailgunFn(string[] args, Seq body, bool isMacro = false)
        {
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
                env.Let(argName, argValue);
                args = tail;
            }

            if (IsVariadic != "")
            {
                env.Let(IsVariadic, Seq.Create(args));
            }
        }

        public object Execute(IEnvironment env, RailgunRuntime runtime, Seq args)
        {
            var nenv = new RailgunEnvironment(env);
            SetupArgs(args, nenv);
            
            var next = Body;
            while (next is Cell c)
            {
                var ev = runtime.Eval(c.Head, nenv);
                if (c.Tail is Nil)
                {
                    return ev;
                }
                next = c.Tail;
            }

            return null;
        }

        
        public Closure BuildClosure(IEnvironment env)
        {
            return new(env, this);
        }
    }

    public class Closure : IRailgunClosure
    {
        private readonly IEnvironment _env;
        private readonly IRailgunFn _func;

        public Closure(IEnvironment env, IRailgunFn func)
        {
            _env = env;
            _func = func;
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return _func.Execute(_env, runtime, args);
        }

        public bool IsMacro => _func.IsMacro;
    }

    public sealed class BuiltinClosure : IRailgunClosure
    {
        public Func<object[], object> Body { get; }
        public bool IsMacro { get; }

        public BuiltinClosure(Func<object[], object> body, bool isMacro = false)
        {
            Body = body;
            IsMacro = isMacro;
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return Body(args.ToArray());
        }
    }
}