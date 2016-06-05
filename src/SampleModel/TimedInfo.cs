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

                var avgExcLowHigh = timings.Skip(1).Take(timings.Count - 2).Average();
                var avgExcLowHighPerMsg = avgExcLowHigh / batchSize;
                var avgExcLowHighPerByte = avgExcLowHighPerMsg / bodySize;
                var avgExcLowHighPerKb = avgExcLowHighPerByte * 1000;
                output($"Avg ms per batch (excl. lowest & highest)\t{avgExcLowHigh}");
                output($"Avg ms per mess (excl. lowest & highest)\t{avgExcLowHighPerMsg}");
                output($"Avg ms per byte (excl. lowest & highest)\t{avgExcLowHighPerByte}");
                output($"Avg ms per kB (excl. lowest & highest)\t{avgExcLowHighPerKb}");

                var isEven = timings.Count % 2 == 0;
                var take = isEven ? 2 : 1;
                var skip = (timings.Count - take) / 2;

                var medExcLowHigh = timings.Skip(skip).Take(take).Average();
                var medExcLowHighPerMsg = medExcLowHigh / batchSize;
                var medExcLowHighPerByte = medExcLowHighPerMsg / bodySize;
                var medExcLowHighPerKb = medExcLowHighPerByte * 1000;
                output($"Median ms per batch\t{medExcLowHigh}");
                output($"Median ms per mess\t{medExcLowHighPerMsg}");
                output($"Median ms per byte\t{medExcLowHighPerByte}");
                output($"Median ms per kB\t{medExcLowHighPerKb}");

                file.Flush();
            }
        }
    }
}