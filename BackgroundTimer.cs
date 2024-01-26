namespace BackgroundTimer;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A timer that runs on a background thread
/// </summary>
public sealed class BackgroundTimer : IDisposable, IAsyncDisposable
{
    private CancellationTokenSource cts;

    /// <summary>
    /// The current tick of the currently running timer
    /// </summary>
    public int CurrentTick { get; private set; }

    /// <summary>
    /// The period on which the timer is currently running, <see cref="TimeSpan.Zero"/> if no timer is running
    /// </summary>
    public TimeSpan Period { get; private set; }

    /// <summary>
    /// The current state of the timer
    /// </summary>
    public BackgroundTimerState State { get; private set; }

    /// <summary>
    /// <see langword="false"/> if the <see cref="State"/> is <see cref="BackgroundTimerState.NotRunning"/>, otherwise <see langword="true"/>
    /// </summary>
    public bool IsRunning => State is not BackgroundTimerState.NotRunning;

    /// <summary>
    /// Starts a new timer with a <see cref="TimeSpan"/>, <see cref="BackgroundTimerCallback"/> and optionally with a <see cref="TimeSpan"/> and returns it
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="stopAtTick">The amount of ticks at which the timer should stop itself</param>
    public static BackgroundTimer StartNew(TimeSpan period, BackgroundTimerCallback callback, TimeSpan? startDelay = null, int stopAtTick = -1)
    {
        var timer = new BackgroundTimer();

        timer.Start(period, callback, startDelay, stopAtTick);

        return timer;
    }

    /// <summary>
    /// Initializes a new background timer
    /// </summary>
    public BackgroundTimer()
    {
        cts = new();

        CurrentTick = 0;
        Period = TimeSpan.Zero;
        State = BackgroundTimerState.NotRunning;
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, a <see cref="BackgroundTimerCallback"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="stopAtTick">The amount of ticks at which the timer should stop itself</param>
    public void Start(TimeSpan period, BackgroundTimerCallback callback, TimeSpan? startDelay = null, int stopAtTick = -1)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;

        Task.Run(async () => await RunAsync(this, period, callback, startDelay, stopAtTick, cts.Token));
    }

    /// <summary>
    /// Stops and resets the timer synchronously
    /// </summary>
    /// <param name="endDelay">The <see cref="TimeSpan"/> the timer waits until it ends</param>
    public BackgroundTimerData Stop(TimeSpan? endDelay = null)
        => StopAsync(endDelay).GetAwaiter().GetResult();

    /// <summary>
    /// Stops and resets the timer asynchronously
    /// </summary>
    /// <param name="endDelay">The <see cref="TimeSpan"/> the timer waits until it ends</param>
    public async Task<BackgroundTimerData> StopAsync(TimeSpan? endDelay = null)
    {
        State = BackgroundTimerState.Stopping;

        if (endDelay.HasValue) await Task.Delay(endDelay.Value);

        await cts.CancelAsync();

        var data = new BackgroundTimerData(CurrentTick, Period);

        Reset();

        return data;
    }

    private void Reset()
    {
        cts.Dispose();
        cts = new();
        CurrentTick = 0;
        Period = TimeSpan.Zero;
        State = BackgroundTimerState.NotRunning;
    }

    private static async Task RunAsync(BackgroundTimer instance, TimeSpan period, BackgroundTimerCallback callback, TimeSpan? startDelay, int stopAtTick, CancellationToken cancelToken)
    {
        if (startDelay.HasValue) await Task.Delay(startDelay.Value, cancelToken);

        instance.State = BackgroundTimerState.Running;

        using (var timer = new PeriodicTimer(period))
        {
            while (await timer.WaitForNextTickAsync(CancellationToken.None))
            {
                if (instance.CurrentTick == stopAtTick) await instance.StopAsync();

                unchecked { instance.CurrentTick++; }
                callback(instance.CurrentTick);

                if (cancelToken.IsCancellationRequested) break;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsRunning) Stop();

        while (!IsRunning) { }

        cts.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (IsRunning) await StopAsync();

        cts.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Callback of the <see cref="BackgroundTimer"/>
/// </summary>
/// <param name="tick">The current timer tick as <see cref="int"/></param>
public delegate void BackgroundTimerCallback(int tick);