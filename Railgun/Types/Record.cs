using System.Collections.Generic;
using System.Linq;
using Railgun.Api;
using Railgun.Runtime;

namespace Railgun.Types
{
    public class StructType : IRailgunClosure
    {
        public Dictionary<string, int> MembersToOffset { get; }
        public StructType(IReadOnlyList<string> members)
        {
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < members.Count; i++)
            {
                dict[members[i]] = i;
            }
            MembersToOffset = dict;
        }

        public object Eval(Seq args)
        {
            return new RailgunRecord(this, args.ToArray());
        }

        public bool IsMacro { get; } = false;
    }

    public class RailgunRecord : IDottable
    {
        public StructType Kind { get; }
        private readonly object[] _members;

        public RailgunRecord(StructType t, object[] members)
        {
            Kind = t;
            _members = members;
        }

        public override string ToString()
        {
            return $"(Name {string.Join(' ', _members.Select(RailgunLibrary.Repr))})";
        }

        public object DotGet(string key)
        {
            return _members[Kind.MembersToOffset[key]];
        }
    }
}