using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;

namespace Railgun.Compiler
{
    public class BlockBuilder : ICompiledEnv
    {
        private readonly ICompiledEnv _parent;
        public readonly ParameterExpression[] ArgsList;
        public readonly Dictionary<string, ParameterExpression> ArgsDict;
        private readonly List<Expression> _expressions = new();

        public readonly Dictionary<string, ParameterExpression> Variables;
        

        public BlockBuilder(ICompiledEnv parent, Dictionary<string, ParameterExpression> vars, IEnumerable<string> args)
        {
            Variables = vars;
            _parent = parent;
            ArgsList = args.Select(x => Expression.Parameter(typeof(object), x)).ToArray();
            ArgsDict = new Dictionary<string, ParameterExpression>();
            foreach (var a in ArgsList)
            {
                ArgsDict[a.Name ?? ""] = a;
            }
        }

        public BlockBuilder(ICompiledEnv parent, Dictionary<string, ParameterExpression> vars):
            this(parent, vars, new List<string>())
        {
        }

        public static Type GetVarFuncType(int arity)
        {
            return arity switch
            {
                0 => typeof(VarFunc<object, object>),
                1 => typeof(VarFunc<object, object, object>),
                2 => typeof(VarFunc<object, object, object, object>),
                3 => typeof(VarFunc<object, object, object, object, object>),
                4 => typeof(VarFunc<object, object, object, object, object, object>),
                _ => throw new NotImplementedException()
            };
        }

        public Expression this[string name]
        {
            get
            {
                // TODO: Add scope climbing
                if (ArgsDict.TryGetValue(name, out var d))
                {
                    return d;
                }
                if (Variables.TryGetValue(name, out var v))
                {
                    return v;
                }
                // Console.WriteLine("this far? " + name);
                return _parent[name];
            }
        }

        public Expression Let(string name, Expression val)
        {
            return Expression.Assign(Variables[name], CheckOrConvert(val, typeof(object)));
        }

        public BlockExpression Build()
        {
            if (_expressions.Count == 0 || _expressions.Last().Type == typeof(void))
            {
                _expressions.Add(Expression.Constant(null, typeof(object)));
            }
            
            return Expression.Block(
                Variables.Select(x => x.Value),
                _expressions
            );
        }

        public LambdaExpression BuildFunction()
        {
            var b = Build();
            if (ArgsList.Length >= 1 && ArgsList.Last()!.Name!.StartsWith('&'))
            {
                var l = Expression.Lambda(GetVarFuncType(ArgsList.Length - 1), b, ArgsList);
                // Console.WriteLine(l.Type);
                return l;
            }
            return Expression.Lambda(b, ArgsList);
        }

        // All Expressions must have values - no void allowed.
        public void AddExpr(Expression ex)
        {
            _expressions.Add(ex);
        }

        public void AddExprs(IEnumerable<Expression> exs)
        {
            foreach (var ex in exs)
            {
                _expressions.Add(ex);
            }
        }

        public static Expression CheckOrConvert(Expression ex, Type t)
        {
            if (ex.Type != t)
            {
                return Expression.ConvertChecked(ex, t);
            }
            return ex;
        }
        
        private static Expression CreateDelegateCall(params Expression[] args)
        {
            var arr = new CSharpArgumentInfo[args.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = CSharpArgumentInfo.Create(default, null);
            }

            var binder = Binder.Invoke(CSharpBinderFlags.None, typeof(object), arr);
            return Expression.Dynamic(binder, typeof(object), args);
        }

        public static Expression GenCall(Expression fn, params Expression[] exprs)
        {
            return CreateDelegateCall(new [] {fn}.Concat(exprs).ToArray());
        }

        public static LoopExpression GenWhile(Expression cond, Expression body)
        {
            cond = CheckOrConvert(cond, typeof(bool));
            var n = Expression.Constant(null, typeof(object));
            var label = Expression.Label(typeof(object));
            return Expression.Loop(
                Expression.IfThenElse(cond, body, Expression.Goto(label, n)),
                label
            );
        }

        public static Expression GenIf(Expression cond, Expression ifExpr, Expression elseExpr = null)
        {
            elseExpr ??= Expression.Constant(null, typeof(object));
            cond = CheckOrConvert(cond, typeof(bool));
            if (ifExpr.Type != elseExpr.Type)
            {
                ifExpr = Expression.ConvertChecked(ifExpr, typeof(object));
                elseExpr = Expression.ConvertChecked(elseExpr, typeof(object));
            }

            // a simple check for cases
            return Expression.Condition(cond, ifExpr, elseExpr);
        }
    }
}