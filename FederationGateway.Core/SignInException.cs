using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core
{
    public class SignInException : Exception
    {
        public SignInException()
        {
        }

        public SignInException(string message)
            : base(message)
        {
        }

        public SignInException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
