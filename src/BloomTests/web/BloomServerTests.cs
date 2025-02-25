﻿// Copyright (c) 2014-2018 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Bloom.Book;
using Bloom.Collection;
using BloomTemp;
using L10NSharp;
using NUnit.Framework;
using SIL.IO;
using Bloom;
using Bloom.ImageProcessing;
using Bloom.Api;
using Bloom.web.controllers;
using SIL.Reporting;
using TemporaryFolder = SIL.TestUtilities.TemporaryFolder;

namespace BloomTests.web
{
	[TestFixture]
	public class BloomServerTests
	{
		private TemporaryFolder _folder;
		private BloomFileLocator _fileLocator;
		private string _collectionPath;
		private ILocalizationManager _localizationManager;

		[SetUp]
		public void Setup()
		{
			Logger.Init();
			_folder = new TemporaryFolder("BloomServerTests");
			LocalizationManager.UseLanguageCodeFolders = true;
			var localizationDirectory = FileLocationUtilities.GetDirectoryDistributedWithApplication("localization");
			_localizationManager = LocalizationManager.Create(TranslationMemory.XLiff, "fr", "Bloom", "Bloom", "1.0.0", localizationDirectory, "SIL/Bloom", null, "", new string[] { });


			ErrorReport.IsOkToInteractWithUser = false;
			_collectionPath = Path.Combine(_folder.Path, "TestCollection");
			var cs = new CollectionSettings(Path.Combine(_folder.Path, "TestCollection.bloomCollection"));
			_fileLocator = new BloomFileLocator(cs, new XMatterPackFinder(new string[] { BloomFileLocator.GetInstalledXMatterDirectory() }), ProjectContext.GetFactoryFileLocations(),
				ProjectContext.GetFoundFileLocations(), ProjectContext.GetAfterXMatterFileLocations());
		}

		[TearDown]
		public void TearDown()
		{
			_localizationManager.Dispose();
			LocalizationManager.ForgetDisposedManagers();
			_folder.Dispose();
			Logger.ShutDown();
		}

		[Test]
		public void CanGetImage()
		{
			// Setup
			using (var server = CreateBloomServer())
			using (var file = MakeTempImage())
			{
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + file.Path);

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
			using (var server = CreateBloomServer())
			using (var file = TempFile.WithExtension(".pdf"))
			{
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + file.Path);

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
			using (var server = CreateBloomServer())
			{
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "/non-existing-file.pdf");

				// Execute
				server.MakeReply(transaction);

				// Verify
				Assert.That(transaction.StatusCode, Is.EqualTo(404));
				Assert.That(Logger.LogText, Contains.Substring("**BloomServer: File Missing: /non-existing-file.pdf"));
			}
		}

		[Test]
		public void SupportsHandlerInjection()
		{
			// Setup
			using (var server = CreateBloomServer())
			{
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "api/thisWontWorkWithoutInjectionButWillWithIt");
				EndpointHandler testFunc = (request) =>
					{
						Assert.That(request.LocalPath(), Does.Contain("thisWontWorkWithoutInjection"));
						Assert.That(request.CurrentCollectionSettings, Is.EqualTo(server.CurrentCollectionSettings));
						request.ReplyWithText("Did It!");
					};
				server.ApiHandler.RegisterEndpointHandler("thisWontWorkWithoutInjection", testFunc, true);

				// Execute
				server.MakeReply(transaction);

				// Verify
				Assert.That(transaction.ReplyContents, Is.EqualTo("Did It!"));
			}
		}

		[Test]
		public void RegisterBoolEndpointHandler_Works()
		{
			// Setup
			using (var server = CreateBloomServer())
			{
				// set boolean handler
				server.ApiHandler.RegisterBooleanEndpointHandler("allowNewBooks",
					// get action
					request => request.CurrentCollectionSettings.AllowDeleteBooks,
					// post action
					(request, myBoolean) => request.CurrentCollectionSettings.AllowNewBooks = myBoolean,
					true);

				// Get
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "api/allowNewBooks");
				server.CurrentCollectionSettings.AllowNewBooks = true;

				// Execute get
				server.MakeReply(transaction);

				// Verify get
				Assert.That(transaction.ReplyContents, Is.EqualTo("true"));

				// Make sure
				server.CurrentCollectionSettings.AllowNewBooks = false;
				server.MakeReply(transaction);
				Assert.That(transaction.ReplyContents, Is.EqualTo("false"));

				// Post
				transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "api/allowNewBooks",
					HttpMethods.Post);
				transaction.SetPostJson("true");

				// Execute post
				server.MakeReply(transaction);

				// Verify post
				Assert.That(transaction.ReplyContents, Is.EqualTo("OK"));
				Assert.That(server.CurrentCollectionSettings.AllowNewBooks, Is.True);

				// Make sure
				transaction.SetPostJson("false");
				server.MakeReply(transaction);
				Assert.That(transaction.ReplyContents, Is.EqualTo("OK"));
				Assert.That(server.CurrentCollectionSettings.AllowNewBooks, Is.False);
			}
		}

		[Test]
		public void RegisterEnumEndpointHandler_Works()
		{
			// Setup
			var info = new BookInfo("", true);
			using (var server = CreateBloomServer(info))
			{
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "api/imageDesc");
				server.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions =
					BookInfo.HowToPublishImageDescriptions.None;
				// set enum handler
				server.ApiHandler.RegisterEnumEndpointHandler("imageDesc",
					// get action
					request => request.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions,
					// post action
					(request, myEnum) => request.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions = myEnum,
					true);

				// Execute get
				server.MakeReply(transaction);

				// Verify get
				Assert.That(transaction.ReplyContents, Is.EqualTo("None"));

				// HowToPublishImageDescriptions.Links was removed in Bloom 4.6
				// Try another
				server.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions =
					BookInfo.HowToPublishImageDescriptions.OnPage;
				server.MakeReply(transaction);
				Assert.That(transaction.ReplyContents, Is.EqualTo("OnPage"));

				// Post
				transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + "api/imageDesc",
					HttpMethods.Post);
				transaction.SetPostJson("OnPage");

				// Execute post
				server.MakeReply(transaction);

				// Verify post
				Assert.That(transaction.ReplyContents, Is.EqualTo("OK"));
				Assert.That(server.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions,
					Is.EqualTo(BookInfo.HowToPublishImageDescriptions.OnPage));

				// Try another
				transaction.SetPostJson("None");
				server.MakeReply(transaction);
				Assert.That(transaction.ReplyContents, Is.EqualTo("OK"));
				Assert.That(server.CurrentBook.BookInfo.MetaData.Epub_HowToPublishImageDescriptions,
					Is.EqualTo(BookInfo.HowToPublishImageDescriptions.None));
			}
		}

		[Test]
		public void Topics_ReturnsFrenchFor_NoTopic_()
		{
			Assert.AreEqual("Aucun thème", QueryServerForJson("api/topics")["NoTopic"].ToString());
		}

		[Test]
		public void Topics_ReturnsFrenchFor_Dictionary_()
		{
			Assert.AreEqual("Dictionnaire", QueryServerForJson("api/topics")["Dictionary"]);
		}

		private Dictionary<string, string> QueryServerForJson(string query)
		{
			using (var server = CreateBloomServer())
			{
				var commonApi = new CommonApi(null,null);
				commonApi.RegisterWithApiHandler(server.ApiHandler);
				var transaction = new PretendRequestInfo(BloomServer.ServerUrlWithBloomPrefixEndingInSlash + query);
				server.MakeReply(transaction);
				Debug.WriteLine(transaction.ReplyContents);
				var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
				return jss.Deserialize<Dictionary<string, string>>(transaction.ReplyContents);
			}
		}

		private BloomServer CreateBloomServer(BookInfo info = null)
		{
			var bookSelection = new BookSelection();
			var collectionSettings = new CollectionSettings();
			bookSelection.SelectBook(new Bloom.Book.Book(info));
			return new BloomServer(new RuntimeImageProcessor(new BookRenamedEvent()), bookSelection, collectionSettings, _fileLocator);
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

		[Test]
		public void CanRetrieveContentOfFakeTempFile_ButOnlyUntilDisposed()
		{
			using (var server = CreateBloomServer())
			{
				var html = @"<html ><head></head><body>here it is</body></html>";
				var dom = new HtmlDom(html);
				dom.BaseForRelativePaths =_folder.Path.ToLocalhost();
				string url;
				using (var fakeTempFile = BloomServer.MakeSimulatedPageFileInBookFolder(dom))
				{
					url = fakeTempFile.Key;
					var transaction = new PretendRequestInfo(url);

					// Execute
					server.MakeReply(transaction);

					// Verify
					// Whitespace inserted by CreateHtml5StringFromXml seems to vary across versions and platforms.
					// I would rather verify the actual output, but don't want this test to be fragile, and the
					// main point is that we get a file with the DOM content.
					Assert.That(transaction.ReplyContents,
						Is.EqualTo(dom.getHtmlStringDisplayOnly()));
				}
				server.DoIdleTasksIfNoActivity();
				var transactionFail = new PretendRequestInfo(url);

				// Execute
				server.MakeReply(transactionFail);

				// Verify
				Assert.That(transactionFail.StatusCode, Is.EqualTo(404));
			}
		}

		[Test]
		[TestCase(BloomServer.SimulatedPageFileSource.Epub, false)]
		[TestCase(BloomServer.SimulatedPageFileSource.Frame, true)]
		[TestCase(BloomServer.SimulatedPageFileSource.Nav, true)]
		[TestCase(BloomServer.SimulatedPageFileSource.Normal, true)]
		[TestCase(BloomServer.SimulatedPageFileSource.Pagelist, false)]
		[TestCase(BloomServer.SimulatedPageFileSource.Preview, true)]
		[TestCase(BloomServer.SimulatedPageFileSource.Pub, true)]
		[TestCase(BloomServer.SimulatedPageFileSource.Thumb, false)]
		public void ServerKnowsDifferenceBetweenRealAndThumbVideos(BloomServer.SimulatedPageFileSource source, bool expectVideo)
		{
			using (var server = CreateBloomServer())
			{
				const string html = @"<html ><head></head><body>
						<div class='bloom-page'>
							<div id='1' class='bloom-videoContainer bloom-noVideoSelected bloom-leadingElement bloom-selected'>
								<video>
									<source src='video/randommp4filename.mp4#t=0.0,4.6'>
									</source>
								</video>
							</div>
							<div class='otherStuff'>
							</div>
							<div id='2' class='bloom-videoContainer'>
								<video>
									<source src='video/otherrandomfilename.mp4'>
									</source>
								</video>
							</div>
							<div id='3' class='bloom-videoContainer bloom-noVideoSelected bloom-leadingElement bloom-selected'>
							</div>
						</div>
						<div class='afterStuff'>
						</div>
					</body></html>";
				var dom = new HtmlDom(html) {BaseForRelativePaths = _folder.Path.ToLocalhost()};
				using (var fakeTempFile = BloomServer.MakeSimulatedPageFileInBookFolder(dom, true, true, source))
				{
					var url = fakeTempFile.Key;
					var transaction = new PretendRequestInfo(url);

					// Execute
					server.MakeReply(transaction);

					// Verify
					var contents = transaction.ReplyContents;
					if (expectVideo)
					{
						AssertThatXmlIn.String(contents).HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class,'bloom-videoContainer')]", 3);
						AssertThatXmlIn.String(contents).HasNoMatchForXpath("//div[contains(@class,'bloom-imageContainer')]");
					}
					else
					{
						AssertThatXmlIn.String(contents).HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class,'bloom-imageContainer')]", 3);
						AssertThatXmlIn.String(contents).HasNoMatchForXpath("//div[contains(@class,'bloom-videoContainer')]");
					}
				}
			}
		}

		[Test]
		public void CanRetrieveContentOfFakeTempFile_WhenFolderContainsAmpersand_ViaJavaScript()
		{
			var dom = SetupDomWithAmpersandInTitle();
			// the 'true' parameter simulates calling BloomServer via JavaScript
			var transaction = CreateServerMakeSimPageMakeReply(dom, true);
			// Verify
			// Whitespace inserted by CreateHtml5StringFromXml seems to vary across versions and platforms.
			// I would rather verify the actual output, but don't want this test to be fragile, and the
			// main point is that we get a file with the DOM content.
			Assert.That(transaction.ReplyContents,
				Is.EqualTo(dom.getHtmlStringDisplayOnly()));
		}

		[Test]
		public void CanRetrieveContentOfFakeTempFile_WhenFolderContainsAmpersand_NotViaJavaScript()
		{
			var dom = SetupDomWithAmpersandInTitle();
			var transaction = CreateServerMakeSimPageMakeReply(dom);
			// Verify
			// Whitespace inserted by CreateHtml5StringFromXml seems to vary across versions and platforms.
			// I would rather verify the actual output, but don't want this test to be fragile, and the
			// main point is that we get a file with the DOM content.
			Assert.That(transaction.ReplyContents,
				Is.EqualTo(dom.getHtmlStringDisplayOnly()));
		}

		private HtmlDom SetupDomWithAmpersandInTitle()
		{
			var ampSubfolder = Path.Combine(_folder.Path, "Using &lt;, &gt;, & &amp; in HTML");
			Directory.CreateDirectory(ampSubfolder);
			var html =
				@"<html ><head><title>Using &lt;lt;, &gt;gt;, &amp; &amp;amp; in HTML</title></head><body>here it is</body></html>";
			var dom = new HtmlDom(html);
			dom.BaseForRelativePaths = ampSubfolder.ToLocalhost();
			return dom;
		}

		private PretendRequestInfo CreateServerMakeSimPageMakeReply(HtmlDom dom, bool simulateCallingFromJavascript = false)
		{
			PretendRequestInfo transaction;
			using (var server = CreateBloomServer())
			{
				using (var fakeTempFile = BloomServer.MakeSimulatedPageFileInBookFolder(dom, simulateCallingFromJavascript))
				{
					var url = fakeTempFile.Key;
					transaction = new PretendRequestInfo(url, forPrinting: false, forSrcAttr: simulateCallingFromJavascript);

					// Execute
					server.MakeReply(transaction);
				}
			}
			return transaction;
		}

		private void SetupCssTests()
		{
			// create collection directory
			Directory.CreateDirectory(_collectionPath);

			// customCollectionStyles.css
			var cssFile = Path.Combine(_collectionPath, "customCollectionStyles.css");
			RobustFile.WriteAllText(cssFile, @".customCollectionStylesCssTest{}");

			// create book directory
			var bookPath = Path.Combine(_collectionPath, "TestBook");
			Directory.CreateDirectory(bookPath);

			// defaultLangStyles.css
			cssFile = Path.Combine(bookPath, "defaultLangStyles.css");
			RobustFile.WriteAllText(cssFile, @".defaultLangStylesCssTest{}");

			cssFile = Path.Combine(bookPath, "customCollectionStyles.css");
			RobustFile.WriteAllText(cssFile, @".customCollectionStylesCssTest{}");

			cssFile = Path.Combine(bookPath, "ForUnitTest-XMatter.css");
			RobustFile.WriteAllText(cssFile, @"This is the one in the book");

			// Factory-XMatter.css
			cssFile = Path.Combine(bookPath, "Factory-XMatter.css");
			RobustFile.WriteAllText(cssFile, @".factoryXmatterCssTest{}");

			// customBookStyles.css
			cssFile = Path.Combine(bookPath, "customBookStyles.css");
			RobustFile.WriteAllText(cssFile, @".customBookStylesCssTest{}");

			// miscStyles.css - a file name not distributed with or created by Bloom
			cssFile = Path.Combine(bookPath, "miscStyles.css");
			RobustFile.WriteAllText(cssFile, @".miscStylesCssTest{}");
		}

		[Test]
		public void GetCorrect_SettingsCollectionStylesCss()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				// Let's do it the way BookStorage.EnsureHasLinksToStylesheets() does it
				var filePath = "defaultLangStyles.css";
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", filePath);

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".defaultLangStylesCssTest{}"));
			}
		}

		[Test]
		public void GetCorrect_SettingsCollectionStylesCss_WhenMakingPdf()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				// Let's do it the way BookStorage.EnsureHasLinksToStylesheets() does it
				var filePath = "defaultLangStyles.css";
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", filePath);

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url, forPrinting: true);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".defaultLangStylesCssTest{}"));
			}
		}

		[Test]
		public void GetCorrect_CustomCollectionStylesCss()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				// Let's do it the way BookStorage.EnsureHasLinksToStylesheets() does it
				var filePath = ".." + Path.DirectorySeparatorChar + "customCollectionStyles.css";
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", filePath);

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".customCollectionStylesCssTest{}"));
			}
		}

		[Test]
		public void GetCorrect_CustomCollectionStylesCss_WhenMakingPdf()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				// Let's do it the way BookStorage.EnsureHasLinksToStylesheets() does it
				var filePath = ".." + Path.DirectorySeparatorChar + "customCollectionStyles.css";
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", filePath);

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url, forPrinting: true);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".customCollectionStylesCssTest{}"));
			}
		}
		[Test]
		public void RequestXMatter_OnlyExistsInBookAndDistFiles_ReturnsTheOneInDistFiles()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", "ForUnitTest-XMatter.css");

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.AreEqual(transaction.ReplyContents.Trim(), "This is the one in DistFiles");
			}
		}
		[Test]
		public void GetCorrect_XmatterStylesCss()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", "Factory-XMatter.css");

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.AreNotEqual(transaction.ReplyContents, ".factoryXmatterCssTest{}");
			}
		}

		[Test]
		public void GetCorrect_CustomBookStylesCss()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", "customBookStyles.css");

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".customBookStylesCssTest{}"));
			}
		}

		[Test]
		public void GetCorrect_CustomBookStylesCss_WhenMakingPdf()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", "customBookStyles.css");

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url, forPrinting: true);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".customBookStylesCssTest{}"));
			}
		}

		[Test]
		public void GetCorrect_MiscStylesCss()
		{
			using (var server = CreateBloomServer())
			{
				SetupCssTests();
				var cssFile = Path.Combine(_folder.Path, "TestCollection", "TestBook", "miscStyles.css");

				var url = cssFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);

				server.MakeReply(transaction);

				Assert.That(transaction.ReplyContents, Is.EqualTo(".miscStylesCssTest{}"));
			}
		}

		[Test]
		public void HandleDoubleEncodedUrls()
		{
			// https://silbloom.myjetbrains.com/youtrack/issue/BL-3835 describes a problem that can occur when
			// Url encoded filenames are stored for the coverImage data.  One of the uploaded books
			// in the library has coverImage data stored as
			// <div data-book="coverImage" lang="*">
			//     The%20Moon%20and%20The%20Cap_Cover.png
			// </div>
			// and the image file was not being found by the server because a second level of encoding was
			// applied before requesting the file.  So this test arbitrarily applies a double level of Url
			// encoding (the third time) to ensure that the server can handle it.
			using (var server = CreateBloomServer())
			{
				Directory.CreateDirectory(_collectionPath);
				var txtFile = Path.Combine(_collectionPath, "File With Spaces.txt");
				const string testData = @"This is a test!\r\n";
				File.WriteAllText(txtFile, testData);

				// no Url encoding of spaces fed to server
				var url = txtFile.ToLocalhost();
				var transaction = new PretendRequestInfo(url);
				server.MakeReply(transaction);
				Assert.That(transaction.ReplyContents, Is.EqualTo(testData));

				// single level of Url encoding fed to server
				var encUrl = txtFile.ToLocalhost().Replace(" ", "%20");		// ToLocalHost() does partial encoding, but not for spaces.
				var encTransaction = new PretendRequestInfo(encUrl);
				Assert.That(encTransaction.RawUrl.Contains("%20"), Is.True);
				server.MakeReply(encTransaction);
				Assert.That(encTransaction.ReplyContents, Is.EqualTo(testData));

				// double level of Url encoding fed to server
				var enc2TxtFile = txtFile.Replace(" ", "%20");		// encodes spaces
				var enc2Url = enc2TxtFile.ToLocalhost();			// double encodes spaces
				var enc2Transaction = new PretendRequestInfo(enc2Url);
				Assert.That(enc2Transaction.RawUrl.Contains("%2520"), Is.True);
				server.MakeReply(enc2Transaction);
				Assert.That(enc2Transaction.ReplyContents, Is.EqualTo(testData));
			}
		}
	}
}
