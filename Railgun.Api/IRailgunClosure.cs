namespace Railgun.Api
{
    public interface IRailgunRuntime
    {
        
    }
    
    public interface IRailgunClosure
    {
        public object Eval(Seq args);
        public bool IsMacro { get; }
    }
}