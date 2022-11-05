using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Loggers
{
    public class TimeLogger : ITimeLogger
    {
        public double ReadingTime { get; set; }
        public double SolvingTime { get; set; }

        public double CommonTime { get => ReadingTime + SolvingTime; }

        public string GetLog()
        {
            var result = string.Empty;

            result += $"Время считывания данных: {ReadingTime} ms\n";
            result += $"Время решения: {SolvingTime} ms\n";
            result += $"Общее время: {CommonTime} ms";

            return result;
        }
    }
}
