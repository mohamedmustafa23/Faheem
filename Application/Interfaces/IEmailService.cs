namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
    }
}