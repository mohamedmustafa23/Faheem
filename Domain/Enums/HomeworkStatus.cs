namespace Domain.Enums
{
    // Per-student homework outcome. Only relevant when the session had homework set.
    public enum HomeworkStatus
    {
        Done = 1,       // عمل
        NotDone = 2,    // ماعملش
        Incomplete = 3  // ناقص
    }
}
