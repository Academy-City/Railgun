namespace Railgun.Types
{
    public record NameExpr(string Name)
    {
        public override string ToString() => Name;
    }

    // yes, I'm bringing this back.
    public class QuoteExpr
    {
        public object Data { get; }

        public QuoteExpr(object data)
        {
            Data = data;
        }

        public object Lower()
        {
            switch (Data)
            {
                case int:
                case long:
                case float:
                case double:
                case string:
                    return Data;
                default:
                    return this;
            }
        }
    }
}