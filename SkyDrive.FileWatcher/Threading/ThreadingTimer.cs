using System;
using System.Threading;

namespace SkyDrive.Threading
{
	public class ThreadingTimer : ITimer, IDisposable
	{
		public event EventHandler Tick;

		private readonly int _interval;
		private readonly Timer _timer;

		public ThreadingTimer(int interval)
		{
			_interval = interval;
			_timer = new Timer(_ => OnTick(), null, Timeout.Infinite, Timeout.Infinite);
		}

		public void Start()
		{
			_timer.Change(0, GetInterval());
		}

		public void Stop()
		{
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

		private void OnTick()
		{
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				var thread = new Thread(() =>
				{
					if (Tick != null)
					{
						Tick(this, new EventArgs());
					}
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			}
			finally
			{
				var interval = GetInterval();
				_timer.Change(interval, interval);
			}
		}

		private int GetInterval()
		{
			return (int)TimeSpan.FromSeconds(_interval).TotalMilliseconds;
		}
	}
}
