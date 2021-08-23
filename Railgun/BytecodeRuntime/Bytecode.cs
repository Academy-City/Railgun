using System;
using System.Collections.Generic;
using System.Linq;
using Railgun.Runtime;
using Railgun.Types;

namespace Railgun.BytecodeRuntime
{
    public interface IByteCode {}
    
    public record Load(string Name) : IByteCode;
    public record Constant(object Value) : IByteCode;

    public record Pop(string Name) : IByteCode;
    public record LetPop(string Name) : IByteCode;

    public record Jump : IByteCode
    {
        public int Location { get; set; }
    }

    public class JumpIfElse : IByteCode
    {
        public int IfTrue { get; set; }
        public int IfFalse { get; set; }
    }
    
    public record Call(int Arity) : IByteCode;
    
    public class CompiledFn : IRailgunFn
    {
        public string[] Args { get; }
        public string IsVariadic { get; } = "";
        public List<IByteCode> Body { get; }
        public bool IsMacro { get; }

        public CompiledFn(string[] args, Seq body, bool isMacro = false)
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

        public object Execute(IEnvironment env, RailgunRuntime runtime, Seq args)
        {
            var nenv = new RailgunEnvironment(env);
            SetupArgs(args, nenv);
            
            // TODO: use the bytecode interpreter
            var stack = new Stack<object>();
            var inst = 0;
            while (inst < Body.Count)
            {
                switch (Body[inst])
                {
                    case Call call:
                        Seq p = Nil.Value;
                        for (var i = 0; i < call.Arity; i++)
                        {
                            p = new Cell(stack.Pop(), p);
                        }

                        var fnToCall = (IRailgunClosure) stack.Pop();
                        var res = fnToCall.Eval(runtime, p);
                        stack.Push(res);
                        break;
                    case Constant constant:
                        stack.Push(constant.Value);
                        break;
                    case Jump jump:
                        inst = jump.Location - 1;
                        break;
                    case JumpIfElse jumpIfElse:
                        inst = ((bool) stack.Pop() ? jumpIfElse.IfTrue : jumpIfElse.IfFalse) - 1;
                        break;
                    case Load load:
                        stack.Push(nenv[load.Name]);
                        break;
                    case Pop pop:
                        var popVal = stack.Pop();
                        if (pop.Name != "_")
                        {
                            nenv.Set(pop.Name, popVal);
                        }
                        break;
                    case LetPop pop:
                        nenv[pop.Name] = stack.Pop();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                inst++;
            }
            var re= stack.Pop();
            return re;
        }

        
        public Closure BuildClosure(IEnvironment env)
        {
            return new(env, this);
        }
    }
}