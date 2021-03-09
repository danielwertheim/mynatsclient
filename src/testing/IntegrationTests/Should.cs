using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;

namespace IntegrationTests
{
    internal static class Should
    {
        internal static void ThrowNatsException(Action a)
            => a.Should().ThrowExactly<NatsException>().Where(ex => ex.ExceptionCode == NatsExceptionCodes.NotConnected);

        internal static async Task ThrowNatsExceptionAsync(Func<Task> a) =>
            (await a.Should().ThrowExactlyAsync<NatsException>()).Where(ex => ex.ExceptionCode == NatsExceptionCodes.NotConnected);
    }
}
