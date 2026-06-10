using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Base controller for all API endpoints.
    ///
    /// ERROR HANDLING DESIGN NOTE:
    /// This project uses exception-based error handling via ErrorHandlingMiddleware.
    /// All service/domain errors are thrown as typed exceptions (NotFoundException,
    /// ConflictException, ForbiddenException, etc.) and caught by the middleware,
    /// which maps them to the correct HTTP status code + ResponseWrapper.Fail().
    ///
    /// Because of this design:
    /// - Handlers ALWAYS return ResponseWrapper<T>.SuccessAsync() — Successful is always true.
    /// - Controllers should ALWAYS return Ok(response) — no conditional checks needed.
    /// - Writing `response.Successful ? Ok(response) : BadRequest(response)` is misleading
    ///   because the BadRequest branch can never be reached at the controller level.
    ///
    /// RULE: Every controller action ends with:  return Ok(response);
    /// </summary>
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        private ISender _sender = null!;

        public ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    }
}