using System;

namespace hlback.ErrorManagement
{
    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message)
        {}
    } // end class DatabaseException()
}