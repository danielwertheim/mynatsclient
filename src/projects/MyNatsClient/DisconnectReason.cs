namespace MyNatsClient
{
    public enum DisconnectReason
    {
        /// <summary>
        /// Disconnect was caused by invoke by user.
        /// </summary>
        ByUser,
        /// <summary>
        /// Disconnect was caused by client failure.
        /// </summary>
        DueToFailure
    }
}