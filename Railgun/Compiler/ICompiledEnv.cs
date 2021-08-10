using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Railgun.Compiler
{
    public interface ICompiledEnv
    {
        Expression this[string name] { get; }
        Expression Let(string name, Expression val);
    }
    
    public class Root : ICompiledEnv
    {
        public Dictionary<string, object> Dict { get; }
        private readonly ParameterExpression _dParam;
        
        private readonly MethodInfo _dMethod = typeof(Dictionary<string, object>).GetMethod("get_Item");
        private readonly MethodInfo _dHasKey = typeof(Dictionary<string, object>).GetMethod("ContainsKey");
        private readonly MethodInfo _dMethodSet = typeof(Dictionary<string, object>).GetMethod("set_Item");


        public Root(Dictionary<string, object> dict)
        {
            Dict = dict;
            _dParam = Expression.Parameter(typeof(Dictionary<string, object>), "globalTable");
        }

        public dynamic WrapAndRun(Expression ex)
        {
            var ll = Expression.Lambda(ex, _dParam);
            var l = ll.Compile();
            return ((dynamic) l)(Dict);
        }

        public Expression this[string name] =>
            Expression.Condition(
                Expression.Call(_dParam, _dHasKey, Expression.Constant(name)),
                Expression.Call(_dParam, _dMethod, Expression.Constant(name)),
                Expression.Block(
                    Expression.Throw(Expression.Constant(new Exception(name))),
                    Expression.Constant(null, typeof(object))
                )
            );

        public Expression Let(string name, Expression val)
        {
            Func<string, object, object> fn = (n, v) =>
            {
                Dict[n] = v;
                return v;
            };
            return BlockBuilder.GenCall(Expression.Constant(fn), Expression.Constant(name), val);

            // return Expression.Call(_dParam, _dMethodSet, Expression.Constant(name), 
            //     BlockBuilder.CheckOrConvert(val, typeof(object)));
        }
    }
}