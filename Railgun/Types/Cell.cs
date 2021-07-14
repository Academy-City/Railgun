using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Railgun.Types
{
    public record Cell(object Head, Cell Tail)
    {
        public Cell Create(IEnumerable<object> list)
        {
            return list.Reverse()
                .Aggregate<object, Cell>(null, (current, item) => new Cell(item, current));
        }
        
        public static IEnumerable<object> Iterate(Cell o)
        {
            while (o != null)
            {
                o = o.Tail;
                yield return o;
            }
        }
    }

    public record SeqExpr(ImmutableList<object> Children)
    {
        public override string ToString()
        {
            return $"({string.Join(' ', Children.Select(x => x.ToString()))})";
        }

        public object Head => Children[0];

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
}