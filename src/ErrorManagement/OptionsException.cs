using System;

namespace hlback.ErrorManagement
{
    public class OptionsException : Exception
    {
        public OptionsException(string message, Exception innerException = null) : base(message, innerException)
        {}
    } // end class OptionsException()
}