using System;

namespace hlback.ErrorManagement
{
    // PathException:
    /// <summary>Class for path-related exceptions in hlback backup processes. [derives from class Exception]</summary>
    public class PathException : Exception
    {
        // PathException constructor:
        /// <summary>Initializes a new <c>PathException</c> object.</summary>
        /// <param name="message">Message describing the error.</param>
        /// <param name="innerException">Optional inner <c>Exception</c> to include in the thrown exception object.</param>
        public PathException(string message, Exception innerException = null) : base(message, innerException)
        {}
    } // end class PathException()
}