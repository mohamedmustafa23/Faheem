using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Features.Students.Commands
{
    public class LeaveGroupCommand : IRequest<Result>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand, Result>
    {
        private readonly IEnrollmentService _enrollmentService;
        public LeaveGroupCommandHandler(IEnrollmentService enrollmentService) => _enrollmentService = enrollmentService;

        public async Task<Result> Handle(LeaveGroupCommand command, CancellationToken cancellationToken)
        {
            var result = await _enrollmentService.LeaveGroupAsync(command.GroupId, command.StudentId, cancellationToken);
            return Result.Success(result);
        }
    }
}
