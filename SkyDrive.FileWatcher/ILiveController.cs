﻿using System.Threading.Tasks;

namespace SkyDrive
{
	public interface ILiveController
	{
		Task<string> GetBlob();
	}
}