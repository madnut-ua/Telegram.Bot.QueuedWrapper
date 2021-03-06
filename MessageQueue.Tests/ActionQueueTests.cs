using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Telegram.Bot.QueuedWrapper;
using Xunit;

namespace MessageQueue.Tests
{
    public class ActionQueueTests
    {
        [Fact]
        public async Task TestTelegramMessageQueueV2()
        {
            var messageQueue = new TelegramMessageQueue();

            var tasks = new List<Task>();
            var runtimes = new List<(int GroupIndex, long Elapsed)>();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int groupIndex = -1; groupIndex > -10; groupIndex--)
            {
                for (int i = 0; i < 80; i++)
                {
                    var grIndex = groupIndex;
                    var iCopy = i;
                    tasks.Add(messageQueue.RunThroughQueue(
                        async () =>
                        {
                          runtimes.Add((grIndex, stopWatch.ElapsedMilliseconds));
                          Debug.WriteLine($"group{grIndex}\t{iCopy}\t" + stopWatch.ElapsedMilliseconds);
                          await Task.Delay(1);
                        },
                        groupIndex.ToString()));
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(messageQueue.RunThroughQueue(
                    async () =>
                    {
                        runtimes.Add((-1, stopWatch.ElapsedMilliseconds));
                        Debug.WriteLine($"user{1}\t{i1}\t" + stopWatch.ElapsedMilliseconds);
                        await Task.Delay(1);
                    }, 
                    (-166).ToString()));
            }

            await Task.WhenAll(tasks);

            var callTimes = runtimes.OrderBy(x => x).ToList();

            var first = callTimes.First();
            var last = callTimes.Last();
            var interval = 950;

            // <= 1 message per second to same group
            for (var i = first.Elapsed; i <= last.Elapsed - interval; i = i + 1)
            {
                callTimes
                   .GroupBy(x => x.GroupIndex)
                   .Select(group => group.Count(x => x.Elapsed >= i && x.Elapsed < i + interval)
                              .Should()
                              .BeLessOrEqualTo(1, $"{group.Key}"))
                   .ToList();
            }

            // <= 30 messages per second
            for (var i = first.Elapsed; i <= last.Elapsed - interval; i = i + 1)
            {
                callTimes
                   .Count(x => x.Elapsed >= i && x.Elapsed < i + interval)
                   .Should()
                   .BeLessOrEqualTo(30);
            }



            // <= 20 message per minute to same group
            interval = 60 * 1000;
            for (var i = first.Elapsed; i <= last.Elapsed - interval; i = i + 1)
            {
                callTimes
                   .GroupBy(x => x.GroupIndex)
                   .Select(group => group.Count(x => x.Elapsed >= i && x.Elapsed < i + interval)
                              .Should()
                              .BeLessOrEqualTo(20))
                   .ToList();
            }

        }


        [Fact]
        public async Task TestQueuedTelegram()
        {
            var messageQueue = new QueuedTelegramBotClient(#"");
            int counter = 0;
            int usercounter = 0;


            var tasks = new List<Func<Task>>();
            var runtimes = new List<(int GroupIndex, long Elapsed)>();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int groupIndex = 0; groupIndex < 1; groupIndex++)
            {
                for (int i = 0; i < 100; i++)
                {
                    var grIndex = groupIndex;
                    tasks.Add(async () =>
                              {
                                  runtimes.Add((grIndex, stopWatch.ElapsedMilliseconds));

                                  Debug.WriteLine(stopWatch.ElapsedMilliseconds);

                                  await messageQueue.SendTextMessageAsync(#"", counter++.ToString());
                              });
                    tasks.Add(async () =>
                              {
                                  runtimes.Add((grIndex, stopWatch.ElapsedMilliseconds));

                                  Debug.WriteLine(stopWatch.ElapsedMilliseconds);

                                  await messageQueue.SendTextMessageAsync(#0, usercounter++.ToString());
                              });
                }
            }

            await Task.WhenAll(tasks.Select(x => x()));
        }
    }
}
