using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Railgun.Api
{
    public abstract class Seq : IEnumerable<object>
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

    public class Nil : Seq
    {
        private Nil() { }
        public static readonly Nil Value = new();
    }

    public class Cell : Seq
    {
        public object Head { get; set; }
        public Seq Tail { get; set; }

        public Cell(object head, Seq tail)
        {
            Head = head;
            Tail = tail;
        }

        public void Deconstruct(out object head, out Seq tail)
        {
            head = Head;
            tail = Tail;
        }
    }

    public record RailgunList(List<object> List);
}