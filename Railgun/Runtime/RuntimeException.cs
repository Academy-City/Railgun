using System;

namespace Railgun.Runtime
{
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message) { }
    }
    
    public class NameException : RuntimeException
    {
        public NameException(string message) : base(message) { }
    }

    public class TypeException : RuntimeException
    {
        public TypeException(string name, string expected) :
            base($"{name} is not a {expected}") { }
    }
}