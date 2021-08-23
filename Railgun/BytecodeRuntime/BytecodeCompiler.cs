using System;
using System.Collections.Generic;
using System.Linq;
using Railgun.Runtime;
using Railgun.Types;

namespace Railgun.BytecodeRuntime
{
    public static class BytecodeCompiler
    {
        public static string ShowBytecode(List<IByteCode> byteCodes)
        {
            return string.Join('\n', byteCodes.Select(x => x switch
            {
                Call call => $"Call {call.Arity}",
                Constant constant => $"Constant {RailgunLibrary.Repr(constant.Value)}",
                Jump jump => $"Jump {jump.Location}",
                JumpIfElse jumpIfElse => $"Jump {jumpIfElse.IfTrue} : {jumpIfElse.IfFalse}",
                Load load => $"Load {load.Name}",
                Pop pop => $"Pop {pop.Name}",
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            }));
        }
        
        public static void CompileExpr(List<IByteCode> byteCodes, object expr)
        {
            if (expr is NameExpr n)
            {
                byteCodes.Add(new Load(n.Name));
                return;
            }
            if (expr is QuoteExpr q)
            {
                byteCodes.Add(new Constant(q.Data));
                return;
            }
            if (expr is Cell {Head: NameExpr { Name: var name }, Tail: var rest})
            {
                switch (name)
                {
                    case "if":
                        var condJump = new JumpIfElse();
                        var endJump = new Jump();
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
                        var wEnd = new Jump();
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