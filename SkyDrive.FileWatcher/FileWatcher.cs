using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SkyDrive
{
	public class FileWatcher : IFileWatcher
	{
		public int Interval { get; private set; }
		public event EventHandler Changed;

		private string _actualSum;
		private string _lastSum;
		private readonly string _filePath;
		private readonly Timer _timer;
		private readonly ILiveController _controller;

		public FileWatcher(string clientId, string path)
			: this(clientId, path, 10) { }

		public FileWatcher(string clientId, string path, int interval)
			: this(new LiveController(clientId), path, interval) { }

		public FileWatcher(ILiveController controller, string path) : this(controller, path, 10) { }

		public FileWatcher(ILiveController controller, string path, int interval)
		{
			_controller = controller;
			_filePath = path;
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
			var blob = await _controller.GetFile(_filePath);
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
