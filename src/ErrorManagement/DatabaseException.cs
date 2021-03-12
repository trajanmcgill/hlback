using System;

namespace hlback.ErrorManagement
{
    // DatabaseException:
    /// <summary>Class for exceptions in hlback database operations. [derives from class Exception]</summary>
    public class DatabaseException : Exception
    {
        // DatabaseException constructor:
        /// <summary>Initializes a new <c>DatabaseException</c> object.</summary>
        /// <param name="message">Message describing the error.</param>
        public DatabaseException(string message) : base(message)
        {}
    } // end class DatabaseException()
}