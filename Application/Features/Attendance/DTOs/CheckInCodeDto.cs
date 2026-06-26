namespace Application.Features.Attendance.DTOs
{
    /// <summary>
    /// A student's short-lived, server-signed check-in code. The student displays it
    /// as a QR; a center scanner reads it and marks the student present in the session
    /// the token names — so the operator never switches between concurrent sessions.
    /// </summary>
    public class CheckInCodeDto
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
