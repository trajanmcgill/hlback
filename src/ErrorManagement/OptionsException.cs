using System;

namespace hlback.ErrorManagement
{
    // OptionsException:
    /// <summary>Class for exceptions encountered processing hlback options. [derives from class Exception]</summary>
    public class OptionsException : Exception
    {
        // OptionsException constructor:
        /// <summary>Initializes a new <c>OptionsException</c> object.</summary>
        /// <param name="message">Message describing the error.</param>
        /// <param name="innerException">Optional inner <c>Exception</c> to include in the thrown exception object.</param>
        public OptionsException(string message, Exception innerException = null) : base(message, innerException)
        {}
    } // end class OptionsException()
}