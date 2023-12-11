namespace BackgroundTimer;

using System;

/// <summary>
/// Contains data of the <see cref="BackgroundTimer"/> instance
/// </summary>
public readonly record struct BackgroundTimerData
{
    /// <summary>
    /// The current tick of the currently running timer
    /// </summary>
    public int CurrentTick { get; }

    /// <summary>
    /// The period on which the timer is currently running, <see cref="TimeSpan.Zero"/> if no timer is running
    /// </summary>
    public TimeSpan Period { get; }

    /// <summary>
    /// The current state of the timer
    /// </summary>
    public BackgroundTimerState State { get; }

    internal BackgroundTimerData(in int currentTick, in TimeSpan period, BackgroundTimerState state)
    {
        CurrentTick = currentTick;
        Period = period;
        State = state;
    }
}