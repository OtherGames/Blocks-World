using System;
using System.Diagnostics;

namespace Iterum.Utils
{
    public class PerfWatch : Stopwatch
    {
        private readonly string _group;
        private double _totalSeconds = 0;
        private PerfWatch(string group)
        {
            _group = group;
        }

        
        public static PerfWatch StartNew(string group = "PerfWatch")
        {
            var perfWatch = new PerfWatch(group);
            perfWatch.Start();
            return perfWatch;
        }

        public PerfWatch LogTotal(string elapsedText = "Total", double minSeconds = 0)
        {
            Stop();
            
            
            double seconds = _totalSeconds + TimeConvert.TicksToSeconds(ElapsedTicks);
            
            if (seconds >= minSeconds)
            {
                InternalLog(elapsedText, seconds, minSeconds);
            }

            return this;
        }


        public double GetSeconds()
        {
            Stop();
            
            double seconds = TimeConvert.TicksToSeconds(ElapsedTicks);
            
            return seconds;
        }
        
        public PerfWatch Log(string elapsedText = "Elapsed", double minSeconds = 0)
        {
            Stop();
            
            double seconds = TimeConvert.TicksToSeconds(ElapsedTicks);

            if (seconds >= minSeconds)
            {
                InternalLog(elapsedText, seconds, minSeconds);

                _totalSeconds += seconds;

            }

            Restart();
            
            return this;
        }

        private void InternalLog(string elapsedText, double seconds, double min)
        {
            var consoleColor = ConsoleColor.DarkGray;
            
            if (seconds >= min) consoleColor = ConsoleColor.Yellow;
            
            if (seconds >= 1f)
            {
                //Logs.Log.Debug(_group, $"{elapsedText}: {seconds:F}s", consoleColor);
                UnityEngine.Debug.Log($"{elapsedText}: {seconds:F}s");
            }
            else
            {
                double mSeconds = TimeConvert.SecondsToMs((float) seconds);
                UnityEngine.Debug.Log($"{elapsedText}: {mSeconds:F}ms");
                //Logs.Log.Debug(_group, $"{elapsedText}: {mSeconds:F}ms", consoleColor);
            }
        }
    }
}