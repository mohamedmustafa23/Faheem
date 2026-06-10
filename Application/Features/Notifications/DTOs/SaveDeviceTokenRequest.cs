namespace Application.Features.Notifications.DTOs
{
    public class SaveDeviceTokenRequest
    {
        public string FcmToken { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? Platform { get; set; } 
    }
}