using System;

namespace SkyDrive
{
	public class AuthResult
	{
		public string AuthorizeCode { get; private set; }
		public string ErrorCode { get; private set; }
		public string ErrorDescription { get; private set; }

		public AuthResult(Uri resultUri)
		{
			string[] queryParams = resultUri.Query.TrimStart('?').Split('&');
			foreach (string param in queryParams)
			{
				string[] kvp = param.Split('=');
				switch (kvp[0])
				{
					case "code":
						AuthorizeCode = kvp[1];
						break;
					case "error":
						ErrorCode = kvp[1];
						break;
					case "error_description":
						ErrorDescription = Uri.UnescapeDataString(kvp[1]);
						break;
				}
			}
		}
	}
}
