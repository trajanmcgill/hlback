using System;

namespace hlback.ErrorManagement
{
    public class PathException : Exception
    {
        public PathException(string message) : base(message)
        {}
    } // end class PathException()
}