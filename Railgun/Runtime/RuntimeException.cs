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
}