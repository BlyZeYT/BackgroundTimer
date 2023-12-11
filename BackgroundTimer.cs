namespace BackgroundTimer;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A timer that runs on a background thread
/// </summary>
public sealed class BackgroundTimer : IDisposable, IAsyncDisposable
{
    private Task _task;
    private CancellationTokenSource _cts;

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
    /// <see langword="true"/> if the <see cref="State"/> is <see cref="BackgroundTimerState.NotRunning"/>, otherwise <see langword="false"/>
    /// </summary>
    public bool IsRunning => State is not BackgroundTimerState.NotRunning;

    /// <summary>
    /// Starts a new timer with a <see cref="TimeSpan"/>, <see cref="BackgroundTimerCallback"/> and optionally with a <see cref="TimeSpan"/> and returns it
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    public static BackgroundTimer StartNew(TimeSpan period, BackgroundTimerCallback callback, TimeSpan? startDelay = null)
    {
        var timer = new BackgroundTimer();

        if (startDelay.HasValue) timer.Start(period, callback, startDelay.Value);
        else timer.Start(period, callback);

        return timer;
    }

    /// <summary>
    /// Starts a new timer with a <see cref="TimeSpan"/>, <see cref="Action"/>[], <see cref="bool"/> and optionally with a <see cref="TimeSpan"/> and returns it
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actionsParallel"><see langword="true"/> if the actions should be invoked parallel, otherwise <see langword="false"/></param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    public static BackgroundTimer StartNew(TimeSpan period, bool actionsParallel, TimeSpan? startDelay = null, params Action[] actions)
    {
        var timer = new BackgroundTimer();

        if (startDelay.HasValue) timer.Start(period, actions, startDelay.Value, actionsParallel);
        else timer.Start(period, actions, actionsParallel);

        return timer;
    }

    /// <summary>
    /// Initializes a new background timer
    /// </summary>
    public BackgroundTimer()
    {
        _task = null!;
        _cts = new();

        CurrentTick = 0;
        Period = TimeSpan.Zero;
        State = BackgroundTimerState.NotRunning;
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/> and <see cref="Action"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="action">The <see cref="Action"/> that should be invoked every tick</param>
    public void Start(TimeSpan period, Action action)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    action.Invoke();
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/> and <see cref="Action"/>[]
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    public void Start(TimeSpan period, Action[] actions)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    foreach (var action in actions) { action.Invoke(); }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/> and <see cref="Action"/>[]
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="actionsParallel"><see langword="true"/> if the actions should be invoked parallel, otherwise <see langword="false"/></param>
    public void Start(TimeSpan period, Action[] actions, bool actionsParallel)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    if (actionsParallel) Parallel.Invoke(actions);
                    else
                    {
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/> and <see cref="BackgroundTimerCallback"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    public void Start(TimeSpan period, BackgroundTimerCallback callback)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/> and <see cref="BackgroundTimerCallback"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="action">The <see cref="Action"/> that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    public void Start(TimeSpan period, Action action, BackgroundTimerCallback callback)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    action.Invoke();
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/>[] and <see cref="BackgroundTimerCallback"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    public void Start(TimeSpan period, Action[] actions, BackgroundTimerCallback callback)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    foreach (var action in actions) { action.Invoke(); }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/>[] and <see cref="BackgroundTimerCallback"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="actionsParallel"><see langword="true"/> if the actions should be invoked parallel, otherwise <see langword="false"/></param>
    public void Start(TimeSpan period, Action[] actions, BackgroundTimerCallback callback, bool actionsParallel)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Running;
        Period = period;
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    if (actionsParallel) Parallel.Invoke(actions);
                    else
                    {
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="action">The <see cref="Action"/> that should be invoked every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    public void Start(TimeSpan period, Action action, TimeSpan startDelay)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    action.Invoke();
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/>[] and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    public void Start(TimeSpan period, Action[] actions, TimeSpan startDelay)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    foreach (var action in actions) { action.Invoke(); }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, an <see cref="Action"/>[] and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="actionsParallel"><see langword="true"/> if the actions should be invoked parallel, otherwise <see langword="false"/></param>
    public void Start(TimeSpan period, Action[] actions, TimeSpan startDelay, bool actionsParallel)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Starting;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    if (actionsParallel) Parallel.Invoke(actions);
                    else
                    {
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, a <see cref="BackgroundTimerCallback"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    public void Start(TimeSpan period, BackgroundTimerCallback callback, TimeSpan startDelay)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, a <see cref="Action"/>, a <see cref="BackgroundTimerCallback"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="action">The <see cref="Action"/> that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    public void Start(TimeSpan period, Action action, BackgroundTimerCallback callback, TimeSpan startDelay)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    action.Invoke();
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, a <see cref="Action"/>[], a <see cref="BackgroundTimerCallback"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    public void Start(TimeSpan period, Action[] actions, BackgroundTimerCallback callback, TimeSpan startDelay)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    foreach (var action in actions) { action.Invoke(); }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Starts the timer with a <see cref="TimeSpan"/>, a <see cref="Action"/>[], a <see cref="BackgroundTimerCallback"/> and <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
    /// <param name="actions">The <see cref="Action"/>[] that should be invoked every tick</param>
    /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
    /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
    /// <param name="actionsParallel"><see langword="true"/> if the actions should be invoked parallel, otherwise <see langword="false"/></param>
    public void Start(TimeSpan period, Action[] actions, BackgroundTimerCallback callback, TimeSpan startDelay, bool actionsParallel)
    {
        if (IsRunning) return;

        State = BackgroundTimerState.Starting;
        Period = period;
        async Task DoStart()
        {
            try
            {
                await Task.Delay(startDelay);

                State = BackgroundTimerState.Running;

                var timer = new PeriodicTimer(period);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    unchecked { CurrentTick++; }
                    callback(CurrentTick);
                    if (actionsParallel) Parallel.Invoke(actions);
                    else
                    {
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        _task = DoStart();
    }

    /// <summary>
    /// Stops the timer
    /// </summary>
    public void Stop() => Task.Run(StopAsync);

    /// <summary>
    /// Stops the timer after the <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="endDelay">The <see cref="TimeSpan"/> the timer waits until it ends</param>
    public void Stop(TimeSpan endDelay) => Task.Run(async () => await StopAsync(endDelay));

    /// <summary>
    /// Stops the timer asynchronously
    /// </summary>
    public async Task StopAsync()
    {
        State = BackgroundTimerState.Stopping;

        _cts.Cancel();

        await _task;

        Reset();
    }

    /// <summary>
    /// Stops the timer asynchronously after the <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="endDelay">The <see cref="TimeSpan"/> the timer waits until it ends</param>
    public async Task StopAsync(TimeSpan endDelay)
    {
        State = BackgroundTimerState.Stopping;

        await Task.Delay(endDelay);

        _cts.Cancel();

        await _task;

        Reset();
    }

    private void Reset()
    {
        _cts.Dispose();
        _task.Dispose();
        _cts = new();
        CurrentTick = 0;
        Period = TimeSpan.Zero;
        State = BackgroundTimerState.NotRunning;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsRunning) Stop();

        while (!IsRunning) { }

        _cts.Dispose();
        _task.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (IsRunning) await StopAsync();

        _cts.Dispose();
        _task.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Callback of the <see cref="BackgroundTimer"/>
/// </summary>
/// <param name="tick">The current timer tick as <see cref="int"/></param>
public delegate void BackgroundTimerCallback(int tick);