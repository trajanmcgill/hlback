using System;

namespace hlback.ErrorManagement
{
    public class PathException : Exception
    {
        public PathException(string message, Exception innerException = null) : base(message, innerException)
        {}
    } // end class PathException()
}