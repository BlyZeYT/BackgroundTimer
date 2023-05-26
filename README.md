<p align="left">
  <img src="https://github.com/BlyZeYT/BackgroundTimer/blob/master/icon.png" height="175">
</p>

# BackgroundTimer
An easy to use timer that runs in the background and provides useful functions like callbacks and states.
For ideas, feedback or anything else just open an issue.

> 🟡 **Project status**: Maintenance mode<sup>[[?]](https://github.com/BlyZeYT/.github/blob/master/project-status.md)</sup>

| Package | NuGet |
| ------- | ----- |
| [BlyZe.BackgroundTimer](https://www.nuget.org/packages/BlyZe.BackgroundTimer) | [![BlyZe.BackgroundTimer](https://img.shields.io/nuget/v/BlyZe.BackgroundTimer?color=white&label=NuGet)](https://www.nuget.org/packages/BlyZe.BackgroundTimer)

## How to use
Initialize a new BackgroundTimer instance.
```
var timer = new BackgroundTimer();
```
## Start the timer how you like to use it
```
timer.Start(TimeSpan.FromMilliseconds(69), () => Console.WriteLine("Running")); //Executes this action every 69 milliseconds

timer.Start(TimeSpan.FromMilliseconds(69), new Action[] //Executes this actions in a row every 69 Milliseconds
{
    () => Console.WriteLine("Action 1"),
    () => Console.WriteLine("Action 2"),
    () => Console.WriteLine("Action 3")
});

timer.Start(TimeSpan.FromMilliseconds(69), new Action[] //Executes this actions parallel every 69 Milliseconds - it is random which Action is executed first, second, usw.
{
    () => Console.WriteLine("Action 1"),
    () => Console.WriteLine("Action 2"),
    () => Console.WriteLine("Action 3")
}, true);
```
## You can create a callback method
```
static void BackgroundTimerCallback(int tick) => Console.WriteLine("Current tick: " + tick); //tick is the current tick of the running timer
```
## Then you can start the timer with that callback method so it get executed on every tick
```
timer.Start(TimeSpan.FromMilliseconds(69), BackgroundTimerCallback); //Starts a timer that executes the callback method every 69 milliseconds
```
## To stop the timer just call the Stop(); or StopAsync(); method
```
timer.Stop(); //Stops the timer

await timer.StopAsync(); //Stops the timer asynchronously

await timer.StopAsync(TimeSpan.FromSeconds(5)); //Stops the timer asynchronously after 5 seconds
```
## You can get information about a timer instance
```
int currentTick = timer.CurrentTick; //Get the current tick the timer is on
BackgroundTimerState currentState = timer.State; //Get the current timer state
TimeSpan timerPeriod = timer.Period; //Get the period the timer is running on
```
# Important Note
> You can only run one timer with one instance. If you want to run multiple timers simultaneously you have to create multiple timer instances.
## Wrong Example
```
var timer = new BackgroundTimer();

timer.Start(TimeSpan.FromMilliseconds(69), BackgroundTimerCallback, TimeSpan.FromSeconds(1.5));
timer.Start(TimeSpan.FromMilliseconds(420), BackgroundTimerCallback); //That timer will not run
```
## Right Example
```
var timer = new BackgroundTimer();

timer.Start(TimeSpan.FromMilliseconds(69), BackgroundTimerCallback, TimeSpan.FromSeconds(1.5)); //Starts the timer

await Task.Delay(2500); //Imitates timer running

//NOTE: You have to use the StopAsync(); method or a while loop to wait for the timer to stop otherwise the timer.Start(); executes before the timer actually stopped!

//First variant - async
if (timer.State is not BackgroundTimerState.NotRunning) await timer.StopAsync();

//Second variant - loop
if (timer.State is not BackgroundTimerState.NotRunning) timer.Stop();
while (timer.State is not BackgroundTimerState.NotRunning) { }

timer.Start(TimeSpan.FromMilliseconds(420), BackgroundTimerCallback); //Start new timer safely
```
