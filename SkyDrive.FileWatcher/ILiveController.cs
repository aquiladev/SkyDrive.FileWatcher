using System.Threading.Tasks;

namespace SkyDrive
{
	public interface ILiveController
	{
		Task<string> GetFile(string path);
		void SaveFile(string path, string value);
	}
}
