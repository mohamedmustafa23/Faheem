using Application.Exceptions;
using Application.Wrappers;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace WebApi
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                Result resultResponse;

                switch (ex)
                {
                    case ConflictException ce:
                        response.StatusCode = (int)ce.StatusCode;
                        resultResponse = Result.Failure(ce.ErrorMessages.ToArray());
                        break;
                    case NotFoundException nfe:
                        response.StatusCode = (int)nfe.StatusCode;
                        resultResponse = Result.Failure(nfe.ErrorMessages.ToArray());
                        break;
                    case ForbiddenException fe:
                        response.StatusCode = (int)fe.StatusCode;
                        resultResponse = Result.Failure(fe.ErrorMessages.ToArray());
                        break;
                    case IdentityException ie:
                        response.StatusCode = (int)ie.StatusCode;
                        resultResponse = Result.Failure(ie.ErrorMessages.ToArray());
                        break;
                    case UnauthorizedException ue:
                        response.StatusCode = (int)ue.StatusCode;
                        resultResponse = Result.Failure(ue.ErrorMessages.ToArray());
                        break;
                    case FluentValidation.ValidationException ve:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        var validationErrors = ve.Errors.Select(e => e.ErrorMessage).ToArray();
                        resultResponse = Result.Failure(validationErrors);
                        break;
                    default:
                        _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        resultResponse = Result.Failure("An unexpected error occurred. Please try again later.");
                        break;
                }

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var resultJson = JsonSerializer.Serialize(resultResponse, options);

                await response.WriteAsync(resultJson);
            }
        }
    }
}