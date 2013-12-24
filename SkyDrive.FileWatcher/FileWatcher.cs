using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SkyDrive
{
	public class LiveWatcher : IFileWatcher
	{
		public int Interval { get; private set; }
		public event EventHandler Changed;

		private string _actualSum;
		private string _lastSum;
		private readonly ILiveController _controller;
		private readonly Timer _timer;

		public LiveWatcher(ILiveController controller) : this(controller, 10) { }

		public LiveWatcher(ILiveController controller, int interval)
		{
			_controller = controller;
			_timer = new Timer(_ => OnTick(), null, Timeout.Infinite, Timeout.Infinite);
			_actualSum = string.Empty;
			_lastSum = string.Empty;
			Interval = interval;
		}

		public void Start()
		{
			_timer.Change(0, GetInterval());
		}

		public void Stop()
		{
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		protected virtual void OnChanged(EventArgs e)
		{
			if (Changed != null)
			{
				Changed(this, e);
			}
		}

		private void OnTick()
		{
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				var thread = new Thread(Checksum);
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
			return (int)TimeSpan.FromSeconds(Interval).TotalMilliseconds;
		}

		private async void Checksum()
		{
			var blob = await _controller.GetBlob();
			if (blob == null)
			{
				return;
			}

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(blob)))
			{
				using (var md5 = MD5.Create())
				{
					_actualSum = BitConverter.ToString(md5.ComputeHash(stream));
				}
			}

			if (_actualSum == _lastSum)
			{
				return;
			}

			OnChanged(new EventArgs());
			_lastSum = _actualSum;
		}
	}
}
