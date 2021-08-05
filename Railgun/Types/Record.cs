using System.Collections.Generic;
using System.Linq;
using Railgun.Runtime;

namespace Railgun.Types
{
    public class RecordType : IRailgunClosure
    {
        public Dictionary<string, int> MembersToOffset { get; }
        public RecordType(IReadOnlyList<string> members)
        {
            // Console.WriteLine(JsonConvert.SerializeObject(members));
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < members.Count; i++)
            {
                dict[members[i]] = i;
            }
            MembersToOffset = dict;
        }

        public object Eval(RailgunRuntime runtime, Seq args)
        {
            return new RailgunRecord(this, args.ToArray());
        }

        public bool IsMacro { get; } = false;
    }

    public class RailgunRecord : IDottable
    {
        public RecordType Kind { get; }
        private readonly object[] _members;

        public RailgunRecord(RecordType t, object[] members)
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