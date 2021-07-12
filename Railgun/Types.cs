using System;
using System.Collections.Immutable;
using System.Linq;

namespace Railgun
{
    public record SeqExpr(ImmutableList<object> Children)
    {
        public override string ToString()
        {
            return $"({string.Join(' ', Children.Select(x => x.ToString()))})";
        }

        public SeqExpr Concat(SeqExpr right)
        {
            return new(Children.Concat(right.Children).ToImmutableList());
        }

        public SeqExpr Map(Func<object, object> fn)
        {
            return new(Children.Select(fn).ToImmutableList());
        }

        public object this[int index] => Children[index];
    }

    public record QuoteExpr(object Value, bool IsQuasiquote = false)
    {
        public override string ToString()
        {
            if (IsQuasiquote)
            {
                return $"`{Value}";
            }

            return $"'{Value}";
        }
    }

    public record UnquoteExpr(object Value)
    {
        public override string ToString()
        {
            return $",{Value}";
        }
    }

    public record NameExpr(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}