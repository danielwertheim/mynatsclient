using System;

namespace MyNatsClient
{
    public class NatsOpMediator : ObservableOf<IOp>, INatsClientStats
    {
        public DateTime LastOpReceivedAt { get; private set; }
        public long OpCount { get; private set; }

        public override void Dispatch(IOp op)
        {
            LastOpReceivedAt = DateTime.UtcNow;
            OpCount++;

            base.Dispatch(op);
        }
    }
}