using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SampleModel
{
    public static class TimedInfo
    {
        public static void Report(string who, List<double> timings, int batchSize, int bodySize)
        {
            if (!timings.Any())
            {
                Console.WriteLine("No timings.");
                return;
            }

            using (var file = File.CreateText($@"D:\Temp\rep-{DateTime.Now.ToString("yyyy-MM-dd")}-{bodySize}-{who}.txt"))
            {
                Action<string> output = s =>
                {
                    Console.WriteLine(s);
                    file.WriteLine(s);
                };

                output("===== RESULT =====");
                output("BatchSize: " + batchSize);
                output("BodySize: " + bodySize);

                timings.ForEach(t => output($"{t}ms"));

                timings.Sort();

                var avgExclLowHighPerBatch = timings.Skip(1).Take(timings.Count - 2).Average();
                var avgExcLowHighPerMsg = avgExclLowHighPerBatch / batchSize;
                var avgExcLowHighPerByte = avgExcLowHighPerMsg / bodySize;
                var avgExcLowHighPerKb = avgExcLowHighPerByte * 1000;
                var avgExcLowHighMsgPerSec = 1000/avgExcLowHighPerMsg;
                var avgExcLowHighKbPerSec = 1000/avgExcLowHighPerKb;

                output($"Avg {avgExclLowHighPerBatch} ms/batch");
                output($"Avg {avgExcLowHighPerMsg} ms/msg");
                output($"Avg {avgExcLowHighPerKb} ms/kb");
                output($"Avg {avgExcLowHighMsgPerSec} msg/s");
                output($"Avg {avgExcLowHighKbPerSec} kb/s");

                var isEven = timings.Count % 2 == 0;
                var take = isEven ? 2 : 1;
                var skip = (timings.Count - take) / 2;

                var medianPerBatch = timings.Skip(skip).Take(take).Average();
                var medianPerMessage = medianPerBatch / batchSize;
                var medianPerByte = medianPerMessage / bodySize;
                var medianPerKb = medianPerByte * 1000;
                var medianMsgPerSec = 1000 / medianPerMessage;
                var medianKbPerSec = 1000 / medianPerKb;

                output($"Median {medianPerBatch} ms/batch");
                output($"Median {medianPerMessage} ms/mess");
                output($"Median {medianPerKb} ms/kb");
                output($"Median {medianMsgPerSec} mess/s");
                output($"Median {medianKbPerSec} kb/s");

                file.Flush();
            }
        }
    }
}