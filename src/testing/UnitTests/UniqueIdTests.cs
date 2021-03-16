using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Internals;
using Xunit;

namespace UnitTests
{
    public class UniqueIdTests : UnitTests
    {
        [Theory]
        [InlineData(1, 1000)]
        [InlineData(2, 500)]
        [InlineData(4, 250)]
        [InlineData(10, 100)]
        public async Task Should_generate_unique_ids(int threadCount, int idCount)
        {
            var ids = new ConcurrentBag<string>();
            var generationTasks = new List<Task>(threadCount);
            for (var taskNumber = 0; taskNumber < threadCount; taskNumber++)
            {
                generationTasks.Add(Task.Run(() =>
                {
                    for (var idNumber = 0; idNumber < idCount; idNumber++)
                    {
                        ids.Add(UniqueId.Generate());
                    }
                }));
            }

            await Task.WhenAll(generationTasks);

            var uniqueIds = new HashSet<string>(ids);
            uniqueIds.Count.Should().Be(ids.Count);
        }
    }
}
