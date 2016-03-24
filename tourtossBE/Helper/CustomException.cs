using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourtoss.BE
{
    /// <summary>
    /// Exception that can be handled in the front end
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class CustomHandledException : Exception
    {
        public object Entity { get; private set; }
        public int ErrorCode { get; private set; }

        public CustomHandledException()
        {
        }

        public CustomHandledException(string message, object entity = null)
            : base(message)
        {
            Entity = entity;
        }

        public CustomHandledException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
