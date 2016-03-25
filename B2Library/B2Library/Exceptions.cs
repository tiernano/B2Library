using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2Library.Exceptions
{
    public class Sha1MisMatchException : Exception
    {
        public Sha1MisMatchException()
        {

        }

        public Sha1MisMatchException(string message) : base(message)
        {

        }

        public Sha1MisMatchException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
