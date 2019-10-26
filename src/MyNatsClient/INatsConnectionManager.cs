using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface INatsConnectionManager
    {
        /// <summary>
        /// Tries to establish a connection to any of the specified hosts in the
        /// sent <see cref="ConnectionInfo"/>.
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Connection and any received <see cref="IOp"/> during the connection phase.</returns>
        Task<Tuple<INatsConnection, IList<IOp>>> OpenConnectionAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken);
    }
}