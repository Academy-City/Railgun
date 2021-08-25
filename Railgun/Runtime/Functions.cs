using System;
using System.Linq;
using Railgun.BytecodeRuntime;
using Railgun.Types;

namespace Railgun.Runtime
{
    public interface IRailgunClosure
    {
        public object Eval(RailgunRuntime runtime, Seq args);
        public bool IsMacro { get; }
    }

    public class Closure : IRailgunClosure
    {
        private readonly IEnvironment _env;
        public RailgunFn Function { get; }

        public Closure(IEnvironment env, RailgunFn function)
        {
            _env = env;
            Function = function;
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return Function.Execute(_env, runtime, args);
        }

        public bool IsMacro => Function.IsMacro;
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