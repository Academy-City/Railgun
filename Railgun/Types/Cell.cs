using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Railgun.Types
{
    public record Cell(object Head, object Tail): IEnumerable<object>
    {
        // TODO: is Null a List?
        public IEnumerator<object> GetEnumerator()
        {
            var current = (object) this;
            while (current is Cell tt)
            {
                yield return tt.Head;
                current = tt.Tail;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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