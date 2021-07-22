using System;
using System.Linq;
using Railgun.Types;

namespace Railgun.Runtime
{
    public interface IRailgunFn
    {
        public object Eval(RailgunRuntime runtime, Seq args);
        public bool IsMacro { get; }
    }

    public class CompiledFunc
    {
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public Seq Body { get; }
        public bool IsMacro { get; }
        
        public CompiledFunc(string[] args, Seq body, bool isMacro = false)
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

        public Closure BuildClosure(IEnvironment env)
        {
            return new(env, this);
        }
    }

    public class Closure : IRailgunFn
    {
        private readonly IEnvironment _env;
        private readonly CompiledFunc _func;

        public Closure(IEnvironment env, CompiledFunc func)
        {
            _env = env;
            _func = func;
        }
        
        private void SetupArgs(Seq args, IEnvironment env)
        {
            var ac = args.Count();
            if (ac != _func.Args.Length && _func.IsVariadic != "" && ac < _func.Args.Length)
            {
                throw new RailgunRuntimeException(
                    $"Wrong number of args: Requested {_func.Args.Length}, Got {ac}");
            }

            foreach (var argName in _func.Args)
            {
                var (argValue, tail) = (Cell) args;
                env[argName] = argValue;
                args = tail;
            }

            if (_func.IsVariadic != "")
            {
                env[_func.IsVariadic] = Seq.Create(args);
            }
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            var env = new RailgunEnvironment(_env);
            SetupArgs(args, env);
            
            var next = _func.Body;
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

        public bool IsMacro => _func.IsMacro;
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
}