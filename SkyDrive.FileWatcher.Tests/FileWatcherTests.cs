using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rhino.Mocks;
using SkyDrive.Threading;

namespace SkyDrive.Tests
{
	[TestFixture]
	public class FileWatcherTests
	{
		private string _path;
		private string _value;
		private int _countOfCalled;
		private ITimer _timer;
		private ILiveController _controller;

		[SetUp]
		public void SetUp()
		{
			_path = @"path\subpath\qwe.as";
			_value = "someValueSomething";
			_countOfCalled = 0;
			_timer = MockRepository.GenerateStub<ITimer>();
			_controller = MockRepository.GenerateMock<ILiveController>();
			_controller.Stub(c => c.GetFile(string.Empty))
				.IgnoreArguments()
				.Return(Task.FromResult(_value));
		}

		[Test]
		public void RaiseTimer_Checksum_CalledGetFileOfController()
		{
			//Arrange
			_controller = MockRepository.GenerateMock<ILiveController>();
			_controller.Expect(c => c.GetFile(_path))
				.Return(new Task<string>(f => string.Empty, CancellationToken.None))
				.Repeat.Once();
			CreateWatcher();

			//Act
			_timer.Raise(t => t.Tick += null, _timer, EventArgs.Empty);

			//Assert
			_controller.VerifyAllExpectations();
		}

		[Test]
		public void RaiseTimer_ChecksumIsCorrect_DontRaiseEvent()
		{
			//Arrange
			CreateWatcher(GetCkecksum(_value));

			//Act
			_timer.Raise(t => t.Tick += null, _timer, EventArgs.Empty);

			//Assert
			Assert.AreEqual(0, _countOfCalled);
		}

		[Test]
		public void RaiseTimer_ChecksumActualValueDifferentThanLast_RaiseEvent()
		{
			//Arrange
			CreateWatcher("blabla");

			//Act
			_timer.Raise(t => t.Tick += null, _timer, EventArgs.Empty);

			//Assert
			Assert.AreEqual(1, _countOfCalled);
		}

		[Test]
		public void Start_CallTimerStart()
		{
			//Arrange
			_timer.Expect(t => t.Start()).Repeat.Once();
			var watcher = CreateWatcher();

			//Act
			watcher.Start();

			//Assert
			_timer.VerifyAllExpectations();
		}

		[Test]
		public void Stop_CallTimerStop()
		{
			//Arrange
			_timer.Expect(t => t.Stop()).Repeat.Once();
			var watcher = CreateWatcher();

			//Act
			watcher.Stop();

			//Assert
			_timer.VerifyAllExpectations();
		}

		#region "Helpers"

		private FileWatcher CreateWatcher(string lastSum = "")
		{
			var watcher = new FileWatcher(_controller, _timer, _path);
			watcher.SetLastSum(lastSum);
			watcher.Changed += (obj, e) => { ++_countOfCalled; };
			return watcher;
		}

		private string GetCkecksum(string data)
		{
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
			{
				using (var md5 = MD5.Create())
				{
					return BitConverter.ToString(md5.ComputeHash(stream));
				}
			}
		}

		#endregion
	}

	internal static class FileWatcherTestExtentions
	{
		public static void SetLastSum(this FileWatcher watcher, string value)
		{
			var field = typeof(FileWatcher).GetField("_lastSum", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
				return;

			field.SetValue(watcher, value);
		}
	}
}
