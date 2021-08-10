using System;

namespace Railgun.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SpecialFormAttribute : Attribute
    {
        public string Name { get; }

        public SpecialFormAttribute(string name)
        {
            Name = name;
        }
    }
}