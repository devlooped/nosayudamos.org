using System;
using System.Net;

namespace NosAyudamos
{
    /// <summary>
    /// Allows event handlers in Core to signal to the HTTP/EventGrid API 
    /// what result should be returned to the caller.
    /// </summary>
    public class HttpStatusException : Exception
    {
        public HttpStatusException(HttpStatusCode statusCode) 
            : base(statusCode.ToString())
            => StatusCode = statusCode;

        public HttpStatusException(HttpStatusCode statusCode, Exception innerException) 
            : base(statusCode.ToString(), innerException)
            => StatusCode = statusCode;

        public HttpStatusCode StatusCode { get; }
    }
}
