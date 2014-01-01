using System;

namespace SkyDrive
{
	public interface IFileWatcher
	{
		event EventHandler Changed;
		void Start();
		void Stop();
	}
}