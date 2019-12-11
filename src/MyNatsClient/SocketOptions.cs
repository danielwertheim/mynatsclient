namespace MyNatsClient
{
    public class SocketOptions
    {
        /// <summary>
        /// Gets or sets the type of address to use for the Socket.
        /// </summary>
        public SocketAddressType? AddressType { get; set; } = SocketAddressType.IpV4;

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
        /// Gets or sets the Recieve timeout in milliseconds for the Socket.
        /// When it times out, the client will look at internal settings
        /// to determine if it should fail or first try and ping the server.
        /// </summary>
        public int? ReceiveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the Send timeout in milliseconds for the Socket.
        /// </summary>
        public int? SendTimeoutMs { get; set; } = 5000;


        /// <summary>
        /// Gets or sets the Connect timeout in milliseconds for the Socket.
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets value indicating if the Nagle algoritm should be used or not
        /// on the created Socket.
        /// </summary>
        public bool? UseNagleAlgorithm { get; set; } = false;
    }
}