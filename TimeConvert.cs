using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Iterum.Utils
{

    public static class TimeConvert
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TicksToSeconds(double ticks) => ticks / (double)Stopwatch.Frequency;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TicksToMs(double ticks) => ticks / (double)Stopwatch.Frequency * 1000.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SecondsToMs(float seconds) => seconds * 1000f;
    }
}