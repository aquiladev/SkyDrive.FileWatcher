﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Live;
using SkyDrive.Threading;

namespace SkyDrive
{
	public class LiveController : IRefreshTokenHandler, ILiveController
	{
		public event EventHandler AuthCanceled;
		public string ClientId { get; private set; }

		private const string EndUrl = "https://login.live.com/oauth20_desktop.srf";

		private AuthForm _authForm;
		private LiveConnectClient _liveConnectClient;
		private RefreshTokenInfo _refreshTokenInfo;
		private bool _isCanceledAccess;
		private readonly bool _ensureFolder;
		private readonly AsyncLock _lock;
		private readonly List<string> _scopes;

		private LiveAuthClient _liveAuthClient;
		public LiveAuthClient AuthClient
		{
			get { return _liveAuthClient ?? (_liveAuthClient = new LiveAuthClient(ClientId, this)); }
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

		public LiveController(string clientId)
			: this(clientId, false) { }

		public LiveController(string clientId, bool ensureFolder)
		{
			ClientId = clientId;
			_ensureFolder = ensureFolder;
			_lock = new AsyncLock();
			_scopes = new List<string>
			{
				"wl.signin",
				"wl.skydrive",
				"wl.skydrive_update",
				"wl.offline_access"
			};
			InitLive();
		}

		#region Implementation IRefreshTokenHandler

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

		#endregion

		public async Task<string> GetFile(string path)
		{
			using (await _lock.LockAsync())
			{
				if (_isCanceledAccess)
				{
					return null;
				}

				if (AuthSession == null)
				{
					SignIn();
				}
				return await ReadFile(path);
			}
		}

		public async void SaveFile(string path, string value)
		{
			using (await _lock.LockAsync())
			{
				if (_isCanceledAccess)
				{
					return;
				}

				if (AuthSession == null)
				{
					SignIn();
				}
				await WriteFile(path, value);
			}
		}

		private async void InitLive()
		{
			var loginResult = await AuthClient.InitializeAsync();
			if (loginResult.Session != null)
			{
				_liveConnectClient = new LiveConnectClient(loginResult.Session);
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
				var session = AuthClient.ExchangeAuthCodeAsync(result.AuthorizeCode);
				_liveConnectClient = new LiveConnectClient(session.Result);
			}
			else
			{
				_isCanceledAccess = true;
				_lock.RejectAsyncs();
				OnAuthCanceled(EventArgs.Empty);
				//this.LogOutput(string.Format("Error received. Error: {0} Detail: {1}", result.ErrorCode, result.ErrorDescription));
			}
		}

		private void OnAuthCanceled(EventArgs e)
		{
			if (AuthCanceled != null)
			{
				AuthCanceled(this, e);
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

		private async Task<string> ReadFile(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path");
			}

			if (_isCanceledAccess)
			{
				return null;
			}

			var livePath = LivePath.Parse(path);
			string folderId;
			if (!string.IsNullOrEmpty(livePath.FilePath))
			{
				folderId = await GetFolderId(livePath);
				if (string.IsNullOrEmpty(folderId))
				{
					throw new LiveFolderNotFoundException(livePath.FilePath);
				}

				folderId = livePath.GetFolderPath(folderId);
			}
			else
			{
				folderId = livePath.SkyDriveFiles;
			}

			var result = await _liveConnectClient.GetAsync(folderId);
			var files = result.Result["data"] as List<object>;
			var file = files == null
				? null
				: files
					.Select(item => item as IDictionary<string, object>)
					.FirstOrDefault(f => f["name"].ToString() == livePath.FileName);

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

		private async Task<LiveOperationResult> WriteFile(string path, string value)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path");
			}

			if (_isCanceledAccess)
			{
				return null;
			}

			var livePath = LivePath.Parse(path);
			string folderId;
			if (!string.IsNullOrEmpty(livePath.FilePath))
			{
				folderId = await GetFolderId(livePath);
				if (string.IsNullOrEmpty(folderId))
				{
					throw new LiveFolderNotFoundException(livePath.FilePath);
				}
			}
			else
			{
				folderId = livePath.SkyDrivePath;
			}

			return await _liveConnectClient.UploadAsync(folderId, livePath.FileName,
				new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)),
				OverwriteOption.Overwrite);
		}

		private async Task<string> GetFolderId(LivePath path)
		{
			string folderId = null;
			var result = await _liveConnectClient.GetAsync(path.SkyDriveFiles);
			if (result != null && !string.IsNullOrEmpty(path.FilePath))
			{
				var items = result.Result["data"] as List<object>;
				folderId = items == null
					? null
					: items.Select(item => item as IDictionary<string, object>)
						.Where(file => file["name"].ToString() == path.PathChain[0])
						.Select(file => file["id"].ToString())
						.FirstOrDefault();

				if (string.IsNullOrEmpty(folderId) && _ensureFolder)
				{
					folderId = await CreateFolder(path.PathChain[0], path.SkyDrivePath);
				}

				if (path.PathChain.Length > 1)
				{
					return await GetFolderIdRecursive(path, folderId, 1);
				}
			}
			return folderId;
		}

		private async Task<string> GetFolderIdRecursive(LivePath path, string folderId, int step)
		{
			while (true)
			{
				string subFolderId = null;
				var result = _liveConnectClient.GetAsync(path.GetFolderPath(folderId)).Result;
				if (result != null)
				{
					var items = result.Result["data"] as List<object>;
					subFolderId = items == null
						? null
						: items.Select(item => item as IDictionary<string, object>)
							.Where(file => file["name"].ToString() == path.PathChain[step])
							.Select(file => file["id"].ToString())
							.FirstOrDefault();

					if (string.IsNullOrEmpty(subFolderId) && _ensureFolder)
					{
						subFolderId = await CreateFolder(path.PathChain[step], folderId);
					}

					if (path.PathChain.Length >= step + 2)
					{
						folderId = subFolderId;
						step = ++step;
						continue;
					}
				}
				return subFolderId;
			}
		}

		private async Task<string> CreateFolder(string folderName, string parentFolder)
		{
			if (string.IsNullOrEmpty(folderName))
			{
				throw new ArgumentNullException("folderName");
			}

			if (string.IsNullOrEmpty(parentFolder))
			{
				throw new ArgumentNullException("parentFolder");
			}

			var folderData = new Dictionary<string, object> { { "name", folderName } };
			var result = await _liveConnectClient.PostAsync(parentFolder, folderData);
			dynamic res = result.Result;
			return res.id;
		}
	}
}
