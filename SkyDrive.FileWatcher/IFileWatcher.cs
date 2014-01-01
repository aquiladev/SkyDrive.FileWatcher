namespace SkyDrive
{
	public interface IFileWatcher
	{
		event FileWatcherEventHandler Changed;
		void Start();
		void Stop();
	}
}