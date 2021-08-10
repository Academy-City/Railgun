namespace Railgun.Types
{
    public record NameExpr(string Name)
    {
        public override string ToString() => Name;
    }

    // yes, I'm bringing this back.
    public record QuoteExpr(object Data);
}