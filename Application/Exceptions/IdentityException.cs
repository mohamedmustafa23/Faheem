using System.Net;

namespace Application.Exceptions
{
    public class IdentityException : Exception
    {
        public List<string> ErrorMessages { get; }
        public HttpStatusCode StatusCode { get; }

        public IdentityException(
            List<string>? errorMessages = null,
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            ErrorMessages = errorMessages ?? [];
            StatusCode = statusCode;
        }
    }
}
