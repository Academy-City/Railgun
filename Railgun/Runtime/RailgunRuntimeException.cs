using System;

namespace Railgun.Runtime
{
    public class RailgunRuntimeException : Exception
    {
        public RailgunRuntimeException(string message) : base(message)
        {
            
        }
    }
}