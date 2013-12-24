using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Live;

namespace SkyDrive
{
	public class LiveController : IRefreshTokenHandler, ILiveController
	{
		public string ClientId { get; private set; }
		public string LocalPath { get; private set; }
		public string FileName { get; private set; }

		private const string SkyDrivePath = "me/skydrive";
		private const string FilesPath = "{0}/files";
		private const string EndUrl = "https://login.live.com/oauth20_desktop.srf";

		private AuthForm _authForm;
		private LiveAuthClient _liveAuthClient;
		private LiveConnectClient _liveConnectClient;
		private RefreshTokenInfo _refreshTokenInfo;
		private readonly AsyncLock _lock = new AsyncLock();

		private readonly List<string> _scopes = new List<string>
		{
			"wl.signin",
			"wl.skydrive",
			"wl.skydrive_update",
			"wl.offline_access"
		};

		private LiveAuthClient AuthClient
		{
			get
			{
				if (_liveAuthClient == null)
				{
					AuthClient = new LiveAuthClient(ClientId, this);
				}
				return _liveAuthClient;
			}
			set
			{
				_liveAuthClient = value;
				_liveConnectClient = null;
			}
		}

		private LiveConnectSession AuthSession
		{
			get
			{
				return AuthClient.Session;
			}
		}

		public LiveController(string clientId, string path)
		{
			ClientId = clientId;
			InitPath(path);
			InitLive();
		}

		private void InitPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path");
			}

			const string separator = @"\";
			var items = path.Split(new[] { separator }, StringSplitOptions.None);
			LocalPath = string.Join(separator, items, 0, items.Length - 1);
			FileName = items[items.Length - 1];
		}

		private async void InitLive()
		{
			var loginResult = await AuthClient.IntializeAsync();
			if (loginResult.Session != null)
			{
				_liveConnectClient = new LiveConnectClient(loginResult.Session);
			}
		}

		private void AuthForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			CleanupAuthForm();
		}

		private void CleanupAuthForm()
		{
			if (_authForm == null)
			{
				return;
			}
			_authForm.Dispose();
			_authForm = null;
		}

		public Task SaveRefreshTokenAsync(RefreshTokenInfo tokenInfo)
		{
			return Task.Factory.StartNew(() =>
			{
				_refreshTokenInfo = tokenInfo;
			});
		}

		public Task<RefreshTokenInfo> RetrieveRefreshTokenAsync()
		{
			return Task.Factory.StartNew(() => _refreshTokenInfo);
		}

		public async Task<string> GetBlob()
		{
			using (await _lock.LockAsync())
			{
				if (AuthSession == null)
				{
					SignIn();
				}
				return await ReadFile();
			}
		}

		public async void SaveBlob(string value)
		{
			using (await _lock.LockAsync())
			{
				if (AuthSession == null)
				{
					SignIn();
				}
				await WriteFile(value);
			}
		}

		private void SignIn()
		{
			if (_authForm != null) return;

			_authForm = new AuthForm(
				AuthClient.GetLoginUrl(_scopes),
				EndUrl,
				OnAuthCompleted);
			_authForm.FormClosed += AuthForm_FormClosed;

			_authForm.ShowDialog();
		}

		private void OnAuthCompleted(AuthResult result)
		{
			CleanupAuthForm();
			if (result.AuthorizeCode != null)
			{
				try
				{
					var session = AuthClient.ExchangeAuthCodeAsync(result.AuthorizeCode);
					_liveConnectClient = new LiveConnectClient(session.Result);
				}
				catch (LiveAuthException aex)
				{
					//this.LogOutput("Failed to retrieve access token. Error: " + aex.Message);
				}
				catch (LiveConnectException cex)
				{
					//this.LogOutput("Failed to retrieve the user's data. Error: " + cex.Message);
				}
			}
			else
			{
				//this.LogOutput(string.Format("Error received. Error: {0} Detail: {1}", result.ErrorCode, result.ErrorDescription));
			}
		}

		private async Task<string> ReadFile()
		{
			var folderId = await GetSyncFolder();
			if (string.IsNullOrEmpty(folderId))
			{
				return null;
			}

			var path = string.Format(FilesPath, folderId);
			var result = await _liveConnectClient.GetAsync(path);
			var files = result.Result["data"] as List<object>;
			var file = files == null
				? null
				: files
					.Select(item => item as IDictionary<string, object>)
					.FirstOrDefault(f => f["name"].ToString() == FileName);

			if (file == null)
			{
				return null;
			}

			var id = file["upload_location"].ToString();
			var fileAsync = await _liveConnectClient.DownloadAsync(id);
			string value;
			using (var reader = new StreamReader(fileAsync.Stream))
			{
				value = reader.ReadToEnd();
			}

			return value;
		}

		private async Task<LiveOperationResult> WriteFile(string value)
		{
			var folderId = await GetSyncFolder();
			if (string.IsNullOrEmpty(folderId))
			{
				return null;
			}

			return await _liveConnectClient.UploadAsync(folderId, FileName,
				new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)),
				OverwriteOption.Overwrite);
		}

		private async Task<string> GetSyncFolder()
		{
			string folderId = null;
			var result = await _liveConnectClient.GetAsync(string.Format(FilesPath, SkyDrivePath));
			if (result != null)
			{
				var items = result.Result["data"] as List<object>;
				folderId = items == null
					? null
					: items.Select(item => item as IDictionary<string, object>)
						.Where(file => file["name"].ToString() == LocalPath)
						.Select(file => file["id"].ToString())
						.FirstOrDefault();

				if (String.IsNullOrEmpty(folderId))
				{
					var folderData = new Dictionary<string, object> { { "name", LocalPath } };
					result = await _liveConnectClient.PostAsync(SkyDrivePath, folderData);
					dynamic res = result.Result;
					folderId = res.id;
				}
			}
			return folderId;
		}
	}
}
