using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Railgun.Types
{
    public abstract record Seq : IEnumerable<object>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<object> GetEnumerator()
        {
            var t = this;
            while (t is Cell c)
            {
                yield return c.Head;
                t = c.Tail;
            }
        }
        
        public static Seq Create(IEnumerable<object> list)
        {
            return list.Reverse()
                .Aggregate<object, Seq>(new Nil(), (current, item) => new Cell(item, current));
        }

    }
    
    public record Nil : Seq;
    public record Cell(object Head, Seq Tail) : Seq;

    // [Obsolete("replace with cellbased lists", true)]
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
}