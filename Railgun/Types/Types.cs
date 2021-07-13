namespace Railgun.Types
{
    public record QuoteExpr(object Value, bool IsQuasiquote = false)
    {
        public override string ToString() =>
            IsQuasiquote ? $"`{Value}" : $"'{Value}";
    }

    public record UnquoteExpr(object Value)
    {
        public override string ToString() => $",{Value}";
    }

    public record NameExpr(string Name)
    {
        public override string ToString() => Name;
    }
}