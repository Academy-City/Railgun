namespace Railgun.Api
{
    public record Keyword(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}