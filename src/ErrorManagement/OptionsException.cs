using System;

namespace hlback.ErrorManagement
{
    public class OptionsException : Exception
    {
        public OptionsException(string message) : base(message)
        {}
    } // end class OptionsException()
}