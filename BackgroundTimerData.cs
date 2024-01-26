namespace BackgroundTimer;

using System;

/// <summary>
/// Contains data of the <see cref="BackgroundTimer"/> instance
/// </summary>
public readonly record struct BackgroundTimerData
{
    /// <summary>
    /// The ticks the timer has completed
    /// </summary>
    public int Ticks { get; }

    /// <summary>
    /// The period on which the timer was running
    /// </summary>
    public TimeSpan Period { get; }

    internal BackgroundTimerData(in int ticks, in TimeSpan period)
    {
        Ticks = ticks;
        Period = period;
    }
}