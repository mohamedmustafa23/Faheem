namespace Application.Features.Payments.DTOs
{
    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? PaidBy { get; set; }
        public string? Notes { get; set; }
    }
}
