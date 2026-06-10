namespace Domain.Enums
{
    public enum NotificationType
    {
        Broadcast = 1,
        SessionUpdated = 2,
        GradePublished = 3,
        AbsenceAlert = 4,
        PaymentConfirmed = 5,
        PaymentDue = 6,
        /// <summary>A new student joined a teacher's group (notification goes to the teacher).</summary>
        StudentJoined = 7,
        /// <summary>Teacher applied a discount on a student's payment record (goes to the student).</summary>
        DiscountApplied = 8
    }
}
