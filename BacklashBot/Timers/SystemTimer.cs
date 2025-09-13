using System.Timers;
using BacklashInterfaces.SmokehouseBot.Timers;

namespace BacklashBot.Timers
{
    /// <summary>
    /// Implementation of ITimer using System.Timers.Timer.
    /// </summary>
    public class SystemTimer : BacklashInterfaces.SmokehouseBot.Timers.ITimer
    {
        private System.Timers.Timer _timer;
        private double _interval = 100.0;
        private bool _autoReset = true;
        private bool _enabled = false;

        /// <summary>
        /// Initializes a new instance of the SystemTimer class.
        /// </summary>
        public SystemTimer()
        {
            _timer = new System.Timers.Timer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = _autoReset;
            _timer.Interval = _interval;
            _timer.Enabled = false;
        }

        /// <summary>
        /// Initializes a new instance of the SystemTimer class with the specified interval.
        /// </summary>
        /// <param name="interval">The interval at which to raise the Elapsed event.</param>
        public SystemTimer(double interval)
        {
            _interval = interval;
            _timer = new System.Timers.Timer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = _autoReset;
            _timer.Interval = _interval;
            _timer.Enabled = false;
        }

        /// <summary>
        /// Occurs when the interval elapses.
        /// </summary>
        public event ElapsedEventHandler Elapsed;

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(this, e);
            if (!_autoReset)
            {
                _enabled = false;
            }
        }

        /// <summary>
        /// Gets or sets the interval at which to raise the Elapsed event.
        /// </summary>
        public double Interval
        {
            get => _interval;
            set
            {
                _interval = value;
                _timer.Interval = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event each time the specified interval elapses or only after the first time.
        /// </summary>
        public bool AutoReset
        {
            get => _autoReset;
            set
            {
                _autoReset = value;
                _timer.AutoReset = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                _timer.Enabled = value;
            }
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            _enabled = true;
            _timer.Start();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            _enabled = false;
            _timer.Stop();
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 32-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to disable periodic signaling.</param>
        public void Change(int dueTime, int period)
        {
            if (period != Timeout.Infinite)
            {
                _timer.Interval = period;
            }
            if (dueTime == 0)
            {
                _timer.Start();
            }
            else if (dueTime == Timeout.Infinite)
            {
                _timer.Stop();
            }
            else
            {
                // Approximate by setting interval and starting
                _timer.Interval = period;
                _timer.Start();
            }
            _enabled = (dueTime != Timeout.Infinite);
        }

        /// <summary>
        /// Releases all resources used by the Timer.
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}