namespace MyNatsClient
{
    /// <summary>
    /// Determines the clients flush behavior
    /// when Pub or PubAsync is called writes.
    /// </summary>
    public enum PubFlushMode
    {
        /// <summary>
        /// Will Flush after each Pub or PubAsync
        /// </summary>
        Auto,
        /// <summary>
        /// Will Flush when you call Flush or FlushAsync
        /// </summary>
        Manual
    }
}