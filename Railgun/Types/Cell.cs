using System;
using System.Collections;
using System.Collections.Generic;
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

        public (object[], Seq) TakeN(int n)
        {
            var r = this;
            var res = new object[n];
            for (var i = 0; i < n; i++)
            {
                var (head, tail) = (Cell) r;
                res[i] = head;
                r = tail;
            }
            return (res, r);
        }

        public Seq Map(Func<object, object> fn)
        {
            return Create(this.Select(fn));
        }

        public static Seq Create(IEnumerable<object> list)
        {
            return list.Reverse()
                .Aggregate<object, Seq>(Nil.Value, (current, item) => new Cell(item, current));
        }
    }

    public record Nil : Seq
    {
        private Nil() { }
        public static readonly Nil Value = new Nil();
    }
    public record Cell(object Head, Seq Tail) : Seq;
}