using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SkyDrive.Threading;

namespace SkyDrive
{
	public class FileWatcher : IFileWatcher
	{
		public event FileWatcherEventHandler Changed;

		private bool _locked;
		private string _actualSum;
		private string _lastSum;
		private readonly string _filePath;
		private readonly ITimer _timer;
		private readonly ILiveController _controller;
		
		public FileWatcher(string clientId, string path, int interval = 10)
			: this(new LiveController(clientId), new ThreadingTimer(interval), path) { }

		public FileWatcher(ILiveController controller, ITimer timer, string path)
		{
			_controller = controller;
			_controller.AuthCanceled += (sender, args) => Lock();
			_timer = timer;
			_timer.Tick += (sender, args) => Checksum();
			_filePath = path;
			_actualSum = string.Empty;
			_lastSum = string.Empty;
		}

		public void Start()
		{
			if (_locked)
			{
				return;
			}

			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
		}

		private void Lock()
		{
			_locked = true;
			_timer.Stop();
		}

		private async void Checksum()
		{
			var data = await _controller.GetFile(_filePath);
			if (data == null)
			{
				return;
			}

			_actualSum = GetChecksum(data);

			if (_actualSum == _lastSum)
			{
				return;
			}

			OnChanged(new FileWatcherEventArgs(data));
			_lastSum = _actualSum;
		}

		private static string GetChecksum(string data)
		{
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
			{
				using (var md5 = MD5.Create())
				{
					return BitConverter.ToString(md5.ComputeHash(stream));
				}
			}
		}

		private void OnChanged(FileWatcherEventArgs e)
		{
			if (Changed != null)
			{
				Changed(this, e);
			}
		}
	}
}
