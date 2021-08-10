namespace Railgun.Compiler
{
    public delegate TResult VarFunc<in TVar, out TResult>(params TVar[] arg);
    public delegate TResult VarFunc<in T1, in TVar, out TResult>(T1 a, params TVar[] arg);
    public delegate TResult VarFunc<in T1, in T2, in TVar, out TResult>(T1 a, T2 b, params TVar[] arg);
    public delegate TResult VarFunc<in T1, in T2, in T3, in TVar, out TResult>(T1 a, T2 b, T3 c, params TVar[] arg);
    public delegate TResult VarFunc<in T1, in T2, in T3, in T4, in TVar, out TResult>
        (T1 a, T2 b, T3 c, T4 d, params TVar[] arg);
    public delegate TResult VarFunc<in T1, in T2, in T3, in T4, in T5, in TVar, out TResult>
        (T1 a, T2 b, T3 c, T4 d, T5 e, params TVar[] arg);
    public delegate TResult VarFunc<in T1, in T2, in T3, in T4, in T5, in T6, in TVar, out TResult>
        (T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, params TVar[] arg); 
    public delegate TResult VarFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in TVar, out TResult>
        (T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, params TVar[] arg);
}