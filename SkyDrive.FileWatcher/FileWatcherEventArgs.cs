using System;

namespace SkyDrive
{
	public delegate void FileWatcherEventHandler(object sender, FileWatcherEventArgs e);

	public class FileWatcherEventArgs : EventArgs
	{
		public string Value { get; private set; }

		public FileWatcherEventArgs(string value)
		{
			Value = value;
		}
	}
}
