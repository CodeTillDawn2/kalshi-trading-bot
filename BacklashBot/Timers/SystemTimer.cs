using System.Timers;
using BacklashInterfaces.SmokehouseBot.Timers;

namespace BacklashBot.Timers
{
    /// <summary>
    /// Implementation of ITimer using System.Timers.Timer.
    /// </summary>
    public class SystemTimer : BacklashInterfaces.SmokehouseBot.Timers.ITimer
    {
        private readonly System.Timers.Timer _timer;

        /// <summary>
        /// Initializes a new instance of the SystemTimer class.
        /// </summary>
        public SystemTimer()
        {
            _timer = new System.Timers.Timer();
        }

        /// <summary>
        /// Initializes a new instance of the SystemTimer class with the specified interval.
        /// </summary>
        /// <param name="interval">The interval at which to raise the Elapsed event.</param>
        public SystemTimer(double interval)
        {
            _timer = new System.Timers.Timer(interval);
        }

        /// <summary>
        /// Occurs when the interval elapses.
        /// </summary>
        public event ElapsedEventHandler Elapsed
        {
            add => _timer.Elapsed += value;
            remove => _timer.Elapsed -= value;
        }

        /// <summary>
        /// Gets or sets the interval at which to raise the Elapsed event.
        /// </summary>
        public double Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event each time the specified interval elapses or only after the first time.
        /// </summary>
        public bool AutoReset
        {
            get => _timer.AutoReset;
            set => _timer.AutoReset = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event.
        /// </summary>
        public bool Enabled
        {
            get => _timer.Enabled;
            set => _timer.Enabled = value;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start() => _timer.Start();

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop() => _timer.Stop();

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 32-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to disable periodic signaling.</param>
        public void Change(double dueTime, double period) => _timer.Change(dueTime, period);

        /// <summary>
        /// Releases all resources used by the Timer.
        /// </summary>
        public void Dispose() => _timer.Dispose();
    }
}