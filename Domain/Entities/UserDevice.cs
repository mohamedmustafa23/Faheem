namespace Domain.Entities
{
    public class UserDevice
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FcmToken { get; set; } = string.Empty;

        public string? DeviceName { get; set; } 
        public string? Platform { get; set; }   

        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}