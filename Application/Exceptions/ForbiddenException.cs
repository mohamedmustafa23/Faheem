using System.Net;

namespace Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        public List<string> ErrorMessages { get; }
        public HttpStatusCode StatusCode { get; }

        public ForbiddenException(
            List<string>? errorMessages = null,
            HttpStatusCode statusCode = HttpStatusCode.Forbidden)
        {
            ErrorMessages = errorMessages ?? [];
            StatusCode = statusCode;
        }
    }
}
