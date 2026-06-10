using System.Net;

namespace Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public List<string> ErrorMessages { get; }
        public HttpStatusCode StatusCode { get; }

        public UnauthorizedException(
            List<string>? errorMessages = null,
            HttpStatusCode statusCode = HttpStatusCode.Unauthorized)
        {
            ErrorMessages = errorMessages ?? [];
            StatusCode = statusCode;
        }
    }

}