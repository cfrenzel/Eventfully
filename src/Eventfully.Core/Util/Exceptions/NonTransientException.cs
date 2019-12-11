using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    [Serializable]
    public class NonTransientException : ApplicationException
    {
        public NonTransientException()
        {

        }

        public NonTransientException(string message, Exception exc = null)
            : base(message, exc)
        {
        }
    }
}