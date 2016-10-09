using System;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public interface INatsExchange
    {
        INatsClient Client { get; }

        /// <summary>
        /// Creates an inbox that consumes <see cref="MsgOp"/> stream
        /// for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this inbox should be subscribed to.</param>
        /// <param name="onIncoming">The handler to invoke when a message is received.</param>
        /// <param name="unsubAfterNMessages">Pass a value to automatically unsubscribe after N messages.</param>
        /// <returns></returns>
        Inbox CreateInbox(string subject, Action<MsgOp> onIncoming, int? unsubAfterNMessages = null);
    }
}