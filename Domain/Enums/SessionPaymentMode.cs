namespace Domain.Enums
{
    public enum SessionPaymentMode
    {
        /// <summary>Bonus/gift session — no payment required.</summary>
        Free,

        /// <summary>
        /// Independent session with its own price.
        /// A separate payment cycle is created for it, completely outside the monthly cycle.
        /// </summary>
        Standalone,

        /// <summary>
        /// Extra session added to the current open monthly cycle.
        /// Adds an ExtraFee on top of the regular MonthlyFee for this cycle.
        /// </summary>
        AddToCycle
    }
}
