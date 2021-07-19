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
}