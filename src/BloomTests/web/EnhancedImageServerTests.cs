﻿// Copyright (c) 2014 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using L10NSharp;
using NUnit.Framework;
using Palaso.IO;
using Palaso.TestUtilities;
using Bloom;
using Bloom.ImageProcessing;
using Bloom.web;
using Palaso.Reporting;
using RestSharp;

namespace BloomTests.web
{
	[TestFixture]
	public class EnhancedImageServerTests
	{
		private TemporaryFolder _folder;

		[SetUp]
		public void Setup()
		{
			Logger.Init();
			_folder = new TemporaryFolder("ImageServerTests");
			var localizationDirectory = FileLocator.GetDirectoryDistributedWithApplication("localization");
			LocalizationManager.Create("fr", "Bloom", "Bloom", "1.0.0", localizationDirectory, "SIL/Bloom", null, "", new string[] { });
		}

		[TearDown]
		public void TearDown()
		{
			_folder.Dispose();
			Logger.ShutDown();
		}

		[Test]
		public void CanGetImage()
		{
			// Setup
			using (var server = CreateImageServer())
			using (var file = MakeTempImage())
			{
				var transaction = new PretendRequestInfo(ServerBase.PathEndingInSlash + file.Path);

				// Execute
				server.MakeReply(transaction);

				// Verify
				Assert.IsTrue(transaction.ReplyImagePath.Contains(".png"));
			}
		}

		[Test]
		public void CanGetPdf()
		{
			// Setup
			using (var server = CreateImageServer())
			using (var file = TempFile.WithExtension(".pdf"))
			{
				var transaction = new PretendRequestInfo(ServerBase.PathEndingInSlash + file.Path);

				// Execute
				server.MakeReply(transaction);

				// Verify
				Assert.IsTrue(transaction.ReplyImagePath.Contains(".pdf"));
			}
		}

		[Test]
		public void ReportsMissingFile()
		{
			// Setup
			using (var server = CreateImageServer())
			{
				var transaction = new PretendRequestInfo(ServerBase.PathEndingInSlash + "/non-existing-file.pdf");

				// Execute
				server.MakeReply(transaction);

				// Verify
				Assert.That(transaction.StatusCode, Is.EqualTo(404));
				Assert.That(Logger.LogText, Contains.Substring("**EnhancedImageServer: File Missing: /non-existing-file.pdf"));
			}
		}


		[Test]
		public void Topics_ReturnsFrenchFor_NoTopic_()
		{
			Assert.AreEqual("Sans thème", QueryServerForJson("topics").NoTopic.ToString());
		}
		[Test]
		public void Topics_ReturnsFrenchFor_Dictionary_()
		{
			Assert.AreEqual("Dictionnaire", QueryServerForJson("topics").Dictionary.ToString());
		}
		private static dynamic QueryServerForJson(string query)
		{
			using (var server = CreateImageServer())
			{
				var transaction = new PretendRequestInfo(ServerBase.PathEndingInSlash + query);
				server.MakeReply(transaction);
				Debug.WriteLine(transaction.ReplyContents);
				return Newtonsoft.Json.JsonConvert.DeserializeObject(transaction.ReplyContents);
			}
		}

		private static EnhancedImageServer CreateImageServer()
		{
			return new EnhancedImageServer(new RuntimeImageProcessor(new BookRenamedEvent()));
		}

		private TempFile MakeTempImage()
		{
			var file = TempFile.WithExtension(".png");
			File.Delete(file.Path);
			using(var x = new Bitmap(100,100))
			{
				x.Save(file.Path, ImageFormat.Png);
			}
			return file;
		}

	}
}
