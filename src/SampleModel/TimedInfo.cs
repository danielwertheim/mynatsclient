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
            using (var file = File.CreateText($@"D:\Temp\rep-{who}-{DateTime.Now.ToString("yyyy-MM-dd-hhmm")}.txt"))
            {
                Action<string> output = s =>
                {
                    Console.WriteLine(s);
                    file.WriteLine(s);
                };

                output("===== RESULT =====");
                output("BatchSize: " + batchSize);
                output("BodySize: " + bodySize);

                var average = timings.Average();
                var averagePerMess = average/batchSize;
                var averagePerByte = averagePerMess/bodySize;
                var averagePerMb = averagePerByte*1000000;

                timings.ForEach(t => output($"{t}ms"));
                output($"Avg ms per batch\t{average}");
                output($"Avg ms per mess\t{averagePerMess}");
                output($"Avg ms per byte\t{averagePerByte}");
                output($"Avg ms per mb\t{averagePerMb}");

                timings.Sort();
                var avgExcLowHigh = timings.Skip(1).Take(timings.Count - 1).Average();
                var avgExcLowHighPerMess = avgExcLowHigh/batchSize;
                var avgExcLowHighPerByte = avgExcLowHighPerMess/bodySize;
                var avgExcLowHighPerMb = avgExcLowHighPerByte*1000000;
                output($"Avg ms per batch (excluding lowest & highest)\t{avgExcLowHigh}");
                output($"Avg ms per mess (excluding lowest & highest)\t{avgExcLowHighPerMess}");
                output($"Avg ms per byte (excluding lowest & highest)\t{avgExcLowHighPerByte}");
                output($"Avg ms per mb (excluding lowest & highest)\t{avgExcLowHighPerMb}");
                file.Flush();
            }
        }
    }
}