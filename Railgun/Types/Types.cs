namespace Railgun.Types
{
    public record NameExpr(string Name)
    {
        public override string ToString() => Name;
    }
}