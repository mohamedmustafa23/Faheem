using System.Net;

namespace Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public List<string> ErrorMessages { get; }
        public HttpStatusCode StatusCode { get; }

        public NotFoundException(
            List<string>? errorMessages = null,
            HttpStatusCode statusCode = HttpStatusCode.NotFound)
        {
            ErrorMessages = errorMessages ?? [];
            StatusCode = statusCode;
        }
    }
}
