using System;

namespace SkyDrive
{
	public interface IFileWatcher
	{
		int Interval { get; }
		event EventHandler Changed;
		void Start();
		void Stop();
	}
}