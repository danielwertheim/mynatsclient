using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class SubscriptionInfoTests : UnitTestsOf<SubscriptionInfo>
    {
        [Theory]
        [InlineData("tests.*")]
        [InlineData("tests.*.test1")]
        [InlineData("tests.foo.*")]
        public void HasWildcardSubject_Should_return_true_When_subject_contains_wildcard(string subject)
        {
            UnitUnderTest = new SubscriptionInfo(subject);

            UnitUnderTest.HasWildcardSubject.Should().BeTrue();
        }

        [Theory]
        [InlineData(">")]
        [InlineData("tests.>")]
        public void HasWildcardSubject_Should_return_true_When_subject_contains_full_wildcard(string subject)
        {
            UnitUnderTest = new SubscriptionInfo(subject);

            UnitUnderTest.HasWildcardSubject.Should().BeTrue();
        }

        [Theory]
        [InlineData("test.>.*.test")]
        [InlineData("test.*.>.test")]
        public void Should_throw_exception_When_constructing_using_both_wildcard_and_full_wildcard(string subject)
        {
            Action test = () => new SubscriptionInfo(subject);

            test.Should().Throw<ArgumentException>()
                .Where(ex => ex.Message.StartsWith("Subject can not contain both the wildcard and full wildcard character."))
                .And.ParamName.Should().Be("subject");
        }

        [Theory]
        [InlineData("test", "test", true)]
        [InlineData("test", "Test", false)]
        public void Matches_Should_match_When_subject_is_equal(string subject, string testSubject, bool expect)
        {
            new SubscriptionInfo(subject).Matches(testSubject).Should().Be(expect);
        }

        [Theory]
        [InlineData(">", "tests.level1.level2", true)]
        [InlineData("tests.>", "tests.level1.level2", true)]
        [InlineData("level1.>", "tests.level1.level2", false)]
        [InlineData("tests.*.level2", "tests.level1.level2", true)]
        [InlineData("tests.*.level2", "tests.level1.level3", false)]
        [InlineData("tests.level1.*", "tests.level1.level2", true)]
        public void Matches_Should_match_When_subject_wildcard_makes_it_match(string subject, string testSubject, bool expect)
        {
            new SubscriptionInfo(subject).Matches(testSubject).Should().Be(expect);
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(2, 500)]
        [InlineData(4, 250)]
        [InlineData(10, 100)]
        public async Task Should_have_unique_id(int threadCount, int subscriptionCount)
        {
            var ids = new ConcurrentBag<string>();
            var generationTasks = new List<Task>(threadCount);
            for (var taskNumber = 0; taskNumber < threadCount; taskNumber++)
            {
                generationTasks.Add(Task.Run(() =>
                {
                    for (var idNumber = 0; idNumber < subscriptionCount; idNumber++)
                    {
                        ids.Add(new SubscriptionInfo("tests.id").Id);
                    }
                }));
            }

            await Task.WhenAll(generationTasks);

            var uniqueIds = new HashSet<string>(ids);
            uniqueIds.Count.Should().Be(ids.Count);
        }
    }
}
