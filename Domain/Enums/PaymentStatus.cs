namespace Domain.Enums
{
    public enum PaymentStatus
    {
        Unpaid        = 1,
        Paid          = 2,
        PartiallyPaid = 3,
        /// <summary>Teacher explicitly waived/forgave this student's payment obligation.</summary>
        Waived        = 4
    }
}