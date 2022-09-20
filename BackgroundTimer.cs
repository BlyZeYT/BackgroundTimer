namespace BackgroundTimer
{
    /// <summary>
    /// A timer that runs on a background thread
    /// </summary>
    public class BackgroundTimer
    {
        private Task _task;
        private CancellationTokenSource _cts = new();

        /// <summary>
        /// The current tick of the currently running timer
        /// </summary>
        public int CurrentTick { get; private set; } = 0;

        /// <summary>
        /// The period on which the timer is currently running, <see cref="TimeSpan.Zero"/> if no timer is running
        /// </summary>
        public TimeSpan Period { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// The current state of the timer
        /// </summary>
        public BackgroundTimerState State { get; private set; } = BackgroundTimerState.NotRunning;

        /// <summary>
        /// Starts a new timer with a <see cref="TimeSpan"/>, <see cref="BackgroundTimerCallback"/> and optionally with a <see cref="TimeSpan"/> and returns it
        /// </summary>
        /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
        /// <param name="startDelay">The <see cref="TimeSpan"/> the timer waits until it starts</param>
        /// <param name="callback">The <see cref="BackgroundTimerCallback"/> that should be executed every tick</param>
        public static BackgroundTimer StartNew(TimeSpan period, BackgroundTimerCallback callback, TimeSpan? startDelay = null)
        {
            BackgroundTimer timer = new();

            if (startDelay is null) timer.Start(period, callback);
            else timer.Start(period, callback, startDelay.Value);

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
            BackgroundTimer timer = new();

            if (startDelay is null) timer.Start(period, actions, actionsParallel);
            else timer.Start(period, actions, startDelay.Value, actionsParallel);

            return timer;
        }

        /// <summary>
        /// Starts the timer with a <see cref="TimeSpan"/> and <see cref="Action"/>
        /// </summary>
        /// <param name="period">The <see cref="TimeSpan"/> on which the timer should run</param>
        /// <param name="action">The <see cref="Action"/> that should be invoked every tick</param>
        public void Start(TimeSpan period, Action action)
        {
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        action.Invoke();
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        if (actionsParallel) Parallel.Invoke(actions);
                        else
                        {
                            foreach (var action in actions) { action.Invoke(); }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        action.Invoke();
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Running;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        if (actionsParallel) Parallel.Invoke(actions);
                        else
                        {
                            foreach (var action in actions) { action.Invoke(); }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

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
                        CurrentTick++;
                        action.Invoke();
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

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
                        CurrentTick++;
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

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
                        CurrentTick++;
                        if (actionsParallel) Parallel.Invoke(actions);
                        else
                        {
                            foreach (var action in actions) { action.Invoke(); }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Starting;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    await Task.Delay(startDelay);

                    State = BackgroundTimerState.Running;

                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Starting;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    await Task.Delay(startDelay);

                    State = BackgroundTimerState.Running;

                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        action.Invoke();
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Starting;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    await Task.Delay(startDelay);

                    State = BackgroundTimerState.Running;

                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        foreach (var action in actions) { action.Invoke(); }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
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
            if (State is not BackgroundTimerState.NotRunning) return;

            State = BackgroundTimerState.Starting;
            Period = period;
            async Task DoStart()
            {
                try
                {
                    await Task.Delay(startDelay);

                    State = BackgroundTimerState.Running;

                    var timer = new PeriodicTimer(period);

                    var ticks = 0;
                    while (await timer.WaitForNextTickAsync(_cts.Token))
                    {
                        CurrentTick++;
                        ticks++;
                        callback(ticks);
                        if (actionsParallel) Parallel.Invoke(actions);
                        else
                        {
                            foreach (var action in actions) { action.Invoke(); }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (OverflowException)
                {
                    await StopAsync();
                }
            }

            _task = DoStart();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public void Stop() => Task.Run(async () => await StopAsync());

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

            _cts.Dispose();

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

            _cts.Dispose();

            Reset();
        }

        private void Reset()
        {
            _cts = new();
            CurrentTick = 0;
            Period = TimeSpan.Zero;
            State = BackgroundTimerState.NotRunning;
        }
    }

    /// <summary>
    /// Callback of the <see cref="BackgroundTimer"/>
    /// </summary>
    /// <param name="tick">The current timer tick as <see cref="int"/></param>
    public delegate void BackgroundTimerCallback(int tick);

    /// <summary>
    /// States of the <see cref="BackgroundTimer"/>
    /// </summary>
    public enum BackgroundTimerState
    {
        /// <summary>
        /// Timer is starting
        /// </summary>
        Starting = 1,
        /// <summary>
        /// Timer is Running
        /// </summary>
        Running,
        /// <summary>
        /// Timer is stopping
        /// </summary>
        Stopping,
        /// <summary>
        /// Timer is not running
        /// </summary>
        NotRunning
    }
}
