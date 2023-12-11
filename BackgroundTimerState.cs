namespace BackgroundTimer;

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
    /// Timer is running
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
