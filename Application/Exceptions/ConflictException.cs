using System.Net;

namespace Application.Exceptions
{
    public class ConflictException : Exception
    {
        public List<string> ErrorMessages { get; }
        public HttpStatusCode StatusCode { get; }

        public ConflictException(
            List<string>? errorMessages = null,
            HttpStatusCode statusCode = HttpStatusCode.Conflict)
        {
            ErrorMessages = errorMessages ?? [];
            StatusCode = statusCode;
        }
    }
}
