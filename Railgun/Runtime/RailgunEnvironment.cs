using System.Collections.Generic;

namespace Railgun.Runtime
{
    public interface IEnvironment
    {
        object this[string key] { get; set; }
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
            get => _dict.TryGetValue(key, out var v) ? v : _parent?[key];
            set => _dict[key] = value;
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