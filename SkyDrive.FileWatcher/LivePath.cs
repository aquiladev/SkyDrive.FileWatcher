using System;
using System.Linq;

namespace SkyDrive
{
	public class LivePath
	{
		public string SkyDrivePath {
			get
			{
				return "me/skydrive";
			}
		}
		public string SkyDriveFiles { get; private set; }
		public string FilePath { get; private set; }
		public string[] PathChain { get; private set; }
		public string FileName { get; private set; }

		private const string FilesTemplate = "{0}/files";
		private const string Separator = @"\";

		public LivePath(string path, string[] pathChain, string fileName)
		{
			FilePath = path;
			PathChain = pathChain;
			FileName = fileName;
			SkyDriveFiles = string.Format(FilesTemplate, SkyDrivePath);
		}

		public string GetFolderPath(string path)
		{
			return string.Format(FilesTemplate, path);
		}

		public static LivePath Parse(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path");
			}

			var items = path.Split(new[] { Separator }, StringSplitOptions.None);
			var pathItems = items.Take(items.Length - 1).ToArray();
			return new LivePath(
				string.Join(Separator, pathItems),
				pathItems,
				items[items.Length - 1]);
		}
	}
}
