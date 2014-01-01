using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SkyDrive.Tests
{
	[TestFixture]
	public class LivePathTests
	{
		private const string FileName = "fileName.jz";
		private const string Path = @"folder\subfolder";
		private const string SkyDrivePath = "me/skydrive";
		private const string FilesTemplate = "{0}/files";

		[Test]
		public void Cstr_CheckSkyDrivePath()
		{
			//Act
			var livePath = new LivePath(string.Empty, string.Empty);

			//Assert
			Assert.AreEqual(SkyDrivePath, livePath.SkyDrivePath);
		}

		[Test]
		public void Cstr_CheckFileName()
		{
			//Act
			var livePath = new LivePath(Path, FileName);

			//Assert
			Assert.AreEqual(FileName, livePath.FileName);
		}

		[Test]
		public void Cstr_CheckFilePath()
		{
			//Act
			var livePath = new LivePath(Path, FileName);

			//Assert
			Assert.AreEqual(Path, livePath.FilePath);
		}

		[Test]
		public void Cstr_CheckPathChain()
		{
			//Arrange
			var pathChain = GetPathChain(Path);

			//Act
			var livePath = new LivePath(Path, FileName);

			//Assert
			CollectionAssert.AreEqual(pathChain, livePath.PathChain);
		}

		[Test]
		public void Cstr_CheckSkyDriveFiles()
		{
			//Arrange
			var expectedPath = string.Format(FilesTemplate, SkyDrivePath);

			//Act
			var livePath = new LivePath(Path, FileName);

			//Assert
			Assert.AreEqual(expectedPath, livePath.SkyDriveFiles);
		}

		[Test]
		public void GetFolderPath_CheckPath()
		{
			//Arrange
			var livePath = new LivePath(string.Empty, string.Empty);

			//Act
			var resultPath = livePath.GetFolderPath(Path);

			//Assert
			Assert.AreEqual(string.Format(FilesTemplate, Path), resultPath);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Parse_CallWithEmptyPath_ThrowException()
		{
			//Act
			LivePath.Parse(string.Empty);
		}

		[Test]
		public void Parse_CheckInstance()
		{
			//Act
			var livePath = LivePath.Parse(string.Format("{0}\\{1}", Path, FileName));

			//Assert
			Assert.AreEqual(FileName, livePath.FileName);
			Assert.AreEqual(Path, livePath.FilePath);
			CollectionAssert.AreEqual(GetPathChain(Path), livePath.PathChain);
		}

		private static IEnumerable<string> GetPathChain(string path)
		{
			return path.Split(new[] { "\\" }, StringSplitOptions.None);
		}
	}
}
