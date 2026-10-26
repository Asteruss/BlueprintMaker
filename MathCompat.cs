using System;

internal static class MathCompat
{
    public static int Clamp(int value, int min, int max) =>
#if NET8_0_OR_GREATER
        Math.Clamp(value, min, max);
#else
        value < min ? min : (value > max ? max : value);
#endif

    public static double Clamp(double value, double min, double max) =>
#if NET8_0_OR_GREATER
        Math.Clamp(value, min, max);
#else
        value < min ? min : (value > max ? max : value);
#endif

    public static float Clamp(float value, float min, float max) =>
#if NET8_0_OR_GREATER
        Math.Clamp(value, min, max);
#else
        value < min ? min : (value > max ? max : value);
#endif

    public static long Clamp(long value, long min, long max) =>
#if NET8_0_OR_GREATER
        Math.Clamp(value, min, max);
#else
        value < min ? min : (value > max ? max : value);
#endif

    public static decimal Clamp(decimal value, decimal min, decimal max) =>
#if NET8_0_OR_GREATER
        Math.Clamp(value, min, max);
#else
        value < min ? min : (value > max ? max : value);
#endif
}