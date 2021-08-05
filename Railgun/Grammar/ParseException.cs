using System;

namespace Railgun.Grammar
{
    public class ParseException : Exception
    {
        public int Index { get; }

        public ParseException(string message, int index) : base(message)
        {
            Index = index;
        }
    }
}