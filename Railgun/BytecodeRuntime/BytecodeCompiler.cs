using System;
using System.Collections.Generic;
using System.Linq;
using Railgun.Runtime;
using Railgun.Types;

namespace Railgun.BytecodeRuntime
{
    public static class BytecodeCompiler
    {
        public static string Decompile(List<IByteCode> byteCodes)
        {
            var b = byteCodes.Select(x => x.GetType().Name + " " + x switch
            {
                Call call => $"{call.Arity}",
                Constant constant => $"{RailgunLibrary.Repr(constant.Value)}",
                CreateClosure createClosure => "",
                Goto jump => $"{jump.Location}",
                JumpIfElse jumpIfElse => $"{jumpIfElse.IfTrue} : {jumpIfElse.IfFalse}",
                LetPop letPop => $"{letPop.Name}",
                Load load => $"{load.Name}",
                Pop pop => $"{pop.Name}",
                _ => ""
            }).Select((x, i) => i.ToString().PadLeft(4) + " | " + x);
            return string.Join('\n', b);
        }

        public static object ExecuteByteCode(List<IByteCode> bytecode, RailgunRuntime rt, IEnvironment nenv)
        {
            var stack = new Stack<object>();
            var inst = 0;
            while (inst < bytecode.Count)
            {
                switch (bytecode[inst])
                {
                    case Call call:
                        Seq p = Nil.Value;
                        for (var i = 0; i < call.Arity; i++)
                        {
                            p = new Cell(stack.Pop(), p);
                        }

                        var fv = stack.Pop();
                        var fnToCall = (IRailgunClosure) fv;
                        var res = fnToCall.Eval(rt, p);
                        stack.Push(res);
                        break;
                    case Constant constant:
                        stack.Push(constant.Value);
                        break;
                    case CreateClosure closure:
                        stack.Push(closure.Fn.BuildClosure(nenv));
                        break;
                    case Goto jump:
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
            return stack.Pop();
        }
        
        public static void CompileExpr(List<IByteCode> byteCodes, object expr)
        {
            switch (expr)
            {
                case NameExpr n:
                    byteCodes.Add(new Load(n.Name));
                    return;
                case QuoteExpr q:
                    byteCodes.Add(new Constant(q.Data));
                    return;
                case Cell {Head: NameExpr { Name: var name }, Tail: var rest}:
                    switch (name)
                    {
                        case "fn":
                        case "macro":
                            var (fnArgs, fnBody) = (Cell) rest;
                            var f = new CompiledFn(
                                ((Seq) fnArgs)
                                .Select(nx => ((NameExpr) nx).Name)
                                .ToArray(),
                                fnBody, name == "macro");
                            byteCodes.Add(new CreateClosure(f));
                            return;
                        case "if":
                            var condJump = new JumpIfElse();
                            var endJump = new Goto();
                            var (ifVars, elseTail) = rest.TakeN(2);
                            CompileExpr(byteCodes, ifVars[0]); // push cond first
                            byteCodes.Add(condJump);

                            condJump.IfTrue = byteCodes.Count;
                            CompileExpr(byteCodes, ifVars[1]);
                            byteCodes.Add(endJump);
                            condJump.IfFalse = byteCodes.Count;
                            if (elseTail is Cell ec)
                            {
                                CompileExpr(byteCodes, ec.Head);
                            }
                            else
                            {
                                byteCodes.Add(new Constant(null));
                            }
                            endJump.Location = byteCodes.Count;
                            return;
                        case "let":
                            var (letVars, _) = rest.TakeN(2);
                            CompileExpr(byteCodes, letVars[1]);
                            byteCodes.Add(new LetPop(((NameExpr) letVars[0]).Name));
                            byteCodes.Add(new Constant(null));
                            return;
                        case "set":
                            var (setVars, _) = rest.TakeN(2);
                            CompileExpr(byteCodes, setVars[1]);
                            byteCodes.Add(new Pop(((NameExpr) setVars[0]).Name));
                            byteCodes.Add(new Constant(null));
                            return;
                        case "while":
                            var (wcond, wbody) = (Cell) rest;
                            var wJump = new JumpIfElse();
                            var wEnd = new Goto();
                            wEnd.Location = byteCodes.Count;
                            CompileExpr(byteCodes, wcond); // push cond first
                            byteCodes.Add(wJump);
                            wJump.IfTrue = byteCodes.Count;
                            foreach (var wexpr in wbody)
                            {
                                CompileExpr(byteCodes, wexpr);
                                byteCodes.Add(new Pop("_"));
                            }
                            byteCodes.Add(wEnd);
                            wJump.IfFalse = byteCodes.Count;
                            byteCodes.Add(new Constant(null));
                            return;
                    }

                    break;
                case Nil:
                    byteCodes.Add(new Constant(Nil.Value));
                    return;
            }

            if (expr is Seq call)
            {
                foreach (var ar in call)
                {
                    CompileExpr(byteCodes, ar);
                }
                byteCodes.Add(new Call(call.Count() - 1));
                return;
            }
            
            // if absolutely nothing, just push a constant
            byteCodes.Add(new Constant(expr));
        }

        public static List<IByteCode> Compile(Seq body)
        {
            var l = new List<IByteCode>();
            foreach (var stmt in body)
            {
                CompileExpr(l, stmt);
                l.Add(new Pop("_"));
            }
            l.RemoveAt(l.Count - 1);
            return l;
        }
    }
}