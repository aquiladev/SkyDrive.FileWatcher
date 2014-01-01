using System;

namespace SkyDrive.Threading
{
	public interface ITimer
	{
		event EventHandler Tick;
		void Start();
		void Stop();
	}
}
