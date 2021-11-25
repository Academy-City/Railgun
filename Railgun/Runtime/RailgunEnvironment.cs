using System.Collections.Generic;

namespace Railgun.Runtime
{
    public interface IEnvironment
    {
        object this[string key] { get; set; }
        bool Exists(string key);
        object Set(string key, object value);
    }
    
    public class RailgunEnvironment : IEnvironment, IDottable
    {
        private readonly IEnvironment _parent;
        private readonly Dictionary<string, object> _dict = new();

        public RailgunEnvironment(IEnvironment parent = null)
        {
            _parent = parent;
        }

        public object this[string key]
        {
            get
            {
                if (_dict.TryGetValue(key, out var v))
                {
                    return v;
                }

                if (_parent == null)
                {
                    throw new NameException($"{key} is not defined");
                }

                return _parent[key];
            }
            set => _dict[key] = value;
        }

        public bool Exists(string key)
        {
            if (_dict.ContainsKey(key)) return true;
            return _parent != null && _parent.Exists(key);
        }

        public object Set(string key, object value)
        {
            if (_dict.ContainsKey(key))
            {
                _dict[key] = value;
            }
            else
            {
                _parent.Set(key, value);
            }
            return value;
        }

        public object DotGet(string key) => this[key];
    }
}