using System;

namespace SkyDrive
{
	public class LiveFolderNotFoundException : Exception
	{
		public LiveFolderNotFoundException(string path) 
			: base(string.Format("Live folder {0} not found", path)) { }
	}
}
