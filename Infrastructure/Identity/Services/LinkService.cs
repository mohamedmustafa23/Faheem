using Application.Exceptions;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Services
{
    public class LinkService : ILinkService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public LinkService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<string> RequestLinkAsync(string parentId, string studentPhone, CancellationToken ct = default)
        {
            var student = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == studentPhone && u.UserType == UserType.Student, ct);
            if (student == null)
                throw new NotFoundException(["No student found with this phone number."]);

            var existingLink = await _dbContext.ParentStudentLinks
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId && l.StudentUserId == student.Id, ct);

            if (existingLink != null)
                throw new ConflictException(["A link request already exists or is already accepted for this student."]);

            var link = new ParentStudentLink
            {
                ParentUserId = parentId,
                StudentUserId = student.Id,
                Status = LinkStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            await _dbContext.ParentStudentLinks.AddAsync(link, ct);
            await _dbContext.SaveChangesAsync(ct);

            return "Link request sent successfully. Waiting for student's confirmation.";
        }

        public async Task<string> RespondToLinkAsync(string studentId, Guid linkId, bool accept, CancellationToken ct = default)
        {
            var link = await _dbContext.ParentStudentLinks
                .FirstOrDefaultAsync(l => l.Id == linkId && l.StudentUserId == studentId, ct);

            if (link == null)
                throw new NotFoundException(["Link request not found."]);

            if (link.Status != LinkStatus.Pending)
                throw new ConflictException(["This request has already been processed."]);

            link.Status = accept ? LinkStatus.Accepted : LinkStatus.Rejected;
            link.ConfirmedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            return accept ? "Parent link accepted successfully." : "Parent link rejected.";
        }

        public async Task<string> UnlinkChildAsync(string parentId, string studentId, CancellationToken ct = default)
        {
            var link = await _dbContext.ParentStudentLinks
                .FirstOrDefaultAsync(l => l.ParentUserId == parentId && l.StudentUserId == studentId, ct);

            if (link == null)
                throw new NotFoundException(["Link not found."]);

            _dbContext.ParentStudentLinks.Remove(link);
            await _dbContext.SaveChangesAsync(ct);

            return "Child unlinked successfully.";
        }

        public async Task<string> UnlinkParentAsync(string studentId, string parentId, CancellationToken ct = default)
        {
            // Same row, just looked up from the student's side. We don't share
            // the parent path because we want the message + caller's identity
            // to be explicit when this gets invoked from the student app.
            var link = await _dbContext.ParentStudentLinks
                .FirstOrDefaultAsync(l => l.StudentUserId == studentId && l.ParentUserId == parentId, ct);

            if (link == null)
                throw new NotFoundException(["Link not found."]);

            _dbContext.ParentStudentLinks.Remove(link);
            await _dbContext.SaveChangesAsync(ct);

            return "Parent unlinked successfully.";
        }
    }
}