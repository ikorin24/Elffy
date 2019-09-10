using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Exceptions
{
    public class SceneParseException : Exception
    {
        internal SceneParseException() : base()
        {
        }

        internal SceneParseException(string message) : base(message)
        {
        }

        internal SceneParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
