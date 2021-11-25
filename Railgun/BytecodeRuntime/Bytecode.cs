using System.Collections.Generic;
using System.Linq;
using Railgun.Api;
using Railgun.Runtime;
using Railgun.Types;

namespace Railgun.BytecodeRuntime
{
    public interface IByteCode {}
    
    public record Load(string Name) : IByteCode;
    public record Constant(object Value) : IByteCode;

    public record Pop(string Name) : IByteCode;
    public record Discard : IByteCode;
    public record LetPop(string Name) : IByteCode;

    public record Goto : IByteCode
    {
        public int Location { get; set; }
    }
    
    public record GotoElse : IByteCode
    {
        public int Location { get; set; }
    }

    public record Call(int Arity) : IByteCode;
    public record CreateClosure(RailgunFn Fn) : IByteCode;
    
    // not bytecode, but similar
    public record StructDefinition(List<string> Members) : IByteCode;
    
    public class RailgunFn
    {
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public List<IByteCode> Body { get; }
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
            Body = BytecodeCompiler.Compile(body);
        }
        
        private void SetupArgs(Seq args, IEnvironment env)
        {
            var ac = args.Count();
            if (ac != Args.Length && IsVariadic != "" && ac < Args.Length)
            {
                throw new RuntimeException(
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
                env[IsVariadic] = args;
            }
        }

        public object Execute(IEnvironment env, Seq args)
        {
            var nenv = new RailgunEnvironment(env);
            SetupArgs(args, nenv);
            return BytecodeCompiler.ExecuteByteCode(Body, nenv);
        }
        
        public Closure BuildClosure(IEnvironment env)
        {
            return new(env, this);
        }
    }
}