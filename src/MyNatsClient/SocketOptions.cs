namespace MyNatsClient
{
    public class SocketOptions
    {
        /// <summary>
        /// Gets or sets the ReceiveBufferSize of the Socket.
        /// Will also adjust the buffer size of the underlying <see cref="System.IO.BufferedStream"/>
        /// that is used by the consumer.
        /// </summary>
        public int? ReceiveBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the SendBufferSize of the Socket.
        /// Will also adjust the buffer size of the underlying <see cref="System.IO.BufferedStream"/>
        /// that is used by the publisher.
        /// </summary>
        public int? SendBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the ReceiveTimeoutMs for the Socket.
        /// When it times out, the client will look at internal settings
        /// to determine if it should fail or first try and ping the server.
        /// </summary>
        public int? ReceiveTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the SendTimeoutMs for the Socket.
        /// </summary>
        public int? SendTimeoutMs { get; set; }
    }
}