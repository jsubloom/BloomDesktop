﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloom;
using Bloom.Book;
using Bloom.WebLibraryIntegration;
using BloomTemp;
using L10NSharp;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.Extensions;
using SIL.Progress;

namespace BloomTests.WebLibraryIntegration
{
	[TestFixture]
	public class BookUploadAndDownloadTests
	{
		private TemporaryFolder _workFolder;
		private string _workFolderPath;
		private BookUpload _uploader;
		private BookDownload _downloader;
		private BloomParseClientTestDouble _parseClient;
		List<BookInfo> _downloadedBooks = new List<BookInfo>();
		private HtmlThumbNailer _htmlThumbNailer;
		private string _thisTestId;

		[SetUp]
		public void Setup()
		{
			_thisTestId = Guid.NewGuid().ToString().Replace('-', '_');

			_workFolder = new TemporaryFolder("unittest-" + _thisTestId);
			_workFolderPath = _workFolder.FolderPath;
			Assert.AreEqual(0,Directory.GetDirectories(_workFolderPath).Count(),"Some stuff was left over from a previous test");
			Assert.AreEqual(0, Directory.GetFiles(_workFolderPath).Count(),"Some stuff was left over from a previous test");
			// Todo: Make sure the S3 unit test bucket is empty.
			// Todo: Make sure the parse.com unit test book table is empty
			_parseClient = new BloomParseClientTestDouble(_thisTestId);
			_htmlThumbNailer = new HtmlThumbNailer(NavigationIsolator.GetOrCreateTheOneNavigationIsolator());
			_uploader = new BookUpload(_parseClient, new BloomS3Client(BloomS3Client.UnitTestBucketName), new BookThumbNailer(_htmlThumbNailer));
			_downloader = new BookDownload(_parseClient, new BloomS3Client(BloomS3Client.UnitTestBucketName), new BookDownloadStartingEvent());
			_downloader.BookDownLoaded += (sender, args) => _downloadedBooks.Add(args.BookDetails);
		}

		[TearDown]
		public void TearDown()
		{
			_htmlThumbNailer.Dispose();
			_workFolder.Dispose();
		}

		private string MakeBook(string bookName, string id, string uploader, string data, bool makeCorruptFile = false)
		{
			var f = new TemporaryFolder(_workFolder, bookName);
			File.WriteAllText(Path.Combine(f.FolderPath, "one.htm"), data);
			File.WriteAllText(Path.Combine(f.FolderPath, "one.css"), @"test");
			if (makeCorruptFile)
				File.WriteAllText(Path.Combine(f.FolderPath, BookStorage.PrefixForCorruptHtmFiles + ".htm"), @"rubbish");

			File.WriteAllText(Path.Combine(f.FolderPath, "meta.json"), "{\"bookInstanceId\":\"" + id + _thisTestId + "\",\"uploadedBy\":\"" + uploader + "\"}");

			return f.FolderPath;
		}

		private void Login()
		{
			Assert.That(_parseClient.TestOnly_LegacyLogIn("unittest@example.com", "unittest"), Is.True,
				"Could not log in using the unittest@example.com account");
		}

		/// <summary>
		/// Review: this is fragile and expensive. We're doing real internet traffic and creating real objects on S3 and parse.com
		/// which (to a very small extent) costs us real money. This will be slow. Also, under S3 eventual consistency rules,
		/// there is no guarantee that the data we just created will actually be retrievable immediately.
		/// </summary>
		/// <param name="bookName"></param>
		/// <param name="id"></param>
		/// <param name="uploader"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public Tuple<string, string> UploadAndDownLoadNewBook(string bookName, string id, string uploader, string data, bool isTemplate = false)
		{
			//  Create a book folder with meta.json that includes an uploader and id and some other files.
			var originalBookFolder = MakeBook(bookName, id, uploader, data, true);
			if (isTemplate)
			{
				var metadata = BookMetaData.FromFolder(originalBookFolder);
				metadata.IsSuitableForMakingShells = true;
				metadata.WriteToFolder(originalBookFolder);
			}
			// The files that actually get uploaded omit some of the ones in the folder.
			// The only omitted one that messes up current unit tests is meta.bak
			var filesToUpload = Directory.GetFiles(originalBookFolder).Where(p => !p.EndsWith(".bak") && !p.Contains(BookStorage.PrefixForCorruptHtmFiles));
			int fileCount = filesToUpload.Count();
			Login();
			//HashSet<string> notifications = new HashSet<string>();

			var progress = new SIL.Progress.StringBuilderProgress();
			var s3Id = _uploader.UploadBook(originalBookFolder,progress);

			var uploadMessages = progress.Text.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

			var expectedFileCount = fileCount + 2; // should get one per file, plus one for metadata, plus one for book order
#if DEBUG
			++expectedFileCount;	// and if in debug mode, then plus one for S3 ID
#endif
			Assert.That(uploadMessages.Length, Is.EqualTo(expectedFileCount), "Uploaded file counts do not match");
			Assert.That(progress.Text, Does.Contain(
				LocalizationManager.GetString("PublishTab.Upload.UploadingBookMetadata", "Uploading book metadata",
				"In this step, Bloom is uploading things like title, languages, & topic tags to the bloomlibrary.org database.")));
			Assert.That(progress.Text, Does.Contain(Path.GetFileName(filesToUpload.First())));

			_uploader.WaitUntilS3DataIsOnServer(BloomS3Client.UnitTestBucketName, originalBookFolder);
			var dest = _workFolderPath.CombineForPath("output");
			Directory.CreateDirectory(dest);
			_downloadedBooks.Clear();
			var url = BookUpload.BloomS3UrlPrefix + BloomS3Client.UnitTestBucketName + "/" + s3Id;
			var newBookFolder = _downloader.HandleDownloadWithoutProgress(url, dest);

			Assert.That(Directory.GetFiles(newBookFolder).Length, Is.EqualTo(fileCount + 1), "Book order was not added during upload"); // book order is added during upload

			Assert.That(_downloadedBooks.Count, Is.EqualTo(1));
			Assert.That(_downloadedBooks[0].FolderPath,Is.EqualTo(newBookFolder));
			// Todo: verify that metadata was transferred to Parse.com
			return new Tuple<string, string>(originalBookFolder, newBookFolder);
		}

		[Test]
		public void BookExists_ExistingBook_ReturnsTrue()
		{
			var someBookPath = MakeBook("local", Guid.NewGuid().ToString(), "someone", "test");
			Login();
			_uploader.UploadBook(someBookPath, new NullProgress());
			Assert.That(_uploader.IsBookOnServer(someBookPath), Is.True);
		}

		[Test]
		public void BookExists_NonExistentBook_ReturnsFalse()
		{
			var localBook = MakeBook("local", "someId", "someone", "test");
			Assert.That(_uploader.IsBookOnServer(localBook), Is.False);
		}

		/// <summary>
		/// Regression test. Using ChangeExtension to append the PDF truncates the name when there is a period.
		/// </summary>
		[Test]
		public void BookWithPeriodInTitle_DoesNotGetTruncatedPdfName()
		{
#if __MonoCS__
			Assert.That(BookUpload.UploadPdfPath("/somewhere/Look at the sky. What do you see"),
				Is.EqualTo("/somewhere/Look at the sky. What do you see/Look at the sky. What do you see.pdf"));
#else
			Assert.That(BookUpload.UploadPdfPath(@"c:\somewhere\Look at the sky. What do you see"),
				Is.EqualTo(@"c:\somewhere\Look at the sky. What do you see\Look at the sky. What do you see.pdf"));
#endif
		}

		[Test]
		public void UploadBooks_SimilarIds_DoNotOverwrite()
		{
			var firstPair = UploadAndDownLoadNewBook("first", "book1", "Jack", "Jack's data");
			var secondPair = UploadAndDownLoadNewBook("second", "book1", "Jill", "Jill's data");
			var thirdPair = UploadAndDownLoadNewBook("third", "book2", "Jack", "Jack's other data");

			// Data uploaded with the same id but a different uploader should form a distinct book; the Jill data
			// should not overwrite the Jack data. Likewise, data uploaded with a distinct Id by the same uploader should be separate.
			var jacksFirstData = File.ReadAllText(firstPair.Item2.CombineForPath("one.htm"));
			// We use stringContaining here because upload does make some changes...for example connected with marking the
			// book as lockedDown.
			Assert.That(jacksFirstData, Does.Contain("Jack's data"));
			var jillsData = File.ReadAllText(secondPair.Item2.CombineForPath("one.htm"));
			Assert.That(jillsData, Does.Contain("Jill's data"));
			var jacksSecondData = File.ReadAllText(thirdPair.Item2.CombineForPath("one.htm"));
			Assert.That(jacksSecondData, Does.Contain("Jack's other data"));

			// Todo: verify that we got three distinct book records in parse.com
		}

		/// <summary>
		/// These two tests were worth writing but I don't think they are worth running every time.
		/// Real uploads are slow and we get occasional failures when S3 is slow or something similar.
		/// Use these tests if you are messing with the upload, download, or locking code.
		/// </summary>
		[Test, Ignore("slow and unlikely to fail")]
		public void RoundTripBookLocksIt()
		{
			var pair = UploadAndDownLoadNewBook("first", "book1", "Jack", "Jack's data");
			var originalFolder = pair.Item1;
			var newFolder = pair.Item2;
			var originalHtml = BookStorage.FindBookHtmlInFolder(originalFolder);
			var newHtml = BookStorage.FindBookHtmlInFolder(newFolder);
			AssertThatXmlIn.HtmlFile(originalHtml).HasNoMatchForXpath("//meta[@name='lockedDownAsShell' and @content='true']");
			AssertThatXmlIn.HtmlFile(newHtml).HasAtLeastOneMatchForXpath("//meta[@name='lockedDownAsShell' and @content='true']");
		}

		[Test, Ignore("slow and unlikely to fail")]
		public void RoundTripOfTemplateBookDoesNotLockIt()
		{
			var pair = UploadAndDownLoadNewBook("first", "book1", "Jack", "Jack's data", true);
			var originalFolder = pair.Item1;
			var newFolder = pair.Item2;
			var originalHtml = BookStorage.FindBookHtmlInFolder(originalFolder);
			var newHtml = BookStorage.FindBookHtmlInFolder(newFolder);
			AssertThatXmlIn.HtmlFile(originalHtml).HasNoMatchForXpath("//meta[@name='lockedDownAsShell' and @content='true']");
			AssertThatXmlIn.HtmlFile(newHtml).HasNoMatchForXpath("//meta[@name='lockedDownAsShell' and @content='true']");
		}

		[Test]
		public void UploadBook_SameId_Replaces()
		{
			var bookFolder = MakeBook("unittest", "myId", "me", "something");
			var jsonPath = bookFolder.CombineForPath(BookInfo.MetaDataFileName);
			var json = File.ReadAllText(jsonPath);
			var jsonStart = json.Substring(0, json.Length - 1);
			var newJson = jsonStart + ",\"bookLineage\":\"original\"}";
			File.WriteAllText(jsonPath, newJson);
			Login();
			string s3Id = _uploader.UploadBook(bookFolder, new NullProgress());
			File.Delete(bookFolder.CombineForPath("one.css"));
			File.WriteAllText(Path.Combine(bookFolder, "one.htm"), "something new");
			File.WriteAllText(Path.Combine(bookFolder, "two.css"), @"test");
			// Tweak the json, but don't change the ID.
			newJson = jsonStart + ",\"bookLineage\":\"other\"}";
			File.WriteAllText(jsonPath, newJson);

			_uploader.UploadBook(bookFolder, new NullProgress());

			var dest = _workFolderPath.CombineForPath("output");
			Directory.CreateDirectory(dest);
			var newBookFolder = _downloader.DownloadBook(BloomS3Client.UnitTestBucketName, s3Id, dest);

			var firstData = File.ReadAllText(newBookFolder.CombineForPath("one.htm"));
			Assert.That(firstData, Does.Contain("something new"), "We should have overwritten the changed file");
			Assert.That(File.Exists(newBookFolder.CombineForPath("two.css")), Is.True, "We should have added the new file");
			Assert.That(File.Exists(newBookFolder.CombineForPath("one.css")), Is.False, "We should have deleted the obsolete file");

			// Verify that metadata was overwritten, new record not created.
			var records = _parseClient.GetBookRecords("myId" + _thisTestId);
			Assert.That(records.Count, Is.EqualTo(1), "Should have overwritten parse server record, not added or deleted");
			var bookRecord = records[0];
			Assert.That(bookRecord.bookLineage.Value, Is.EqualTo("other"));
		}

		[Test]
		public void UploadBook_SetsInterestingParseFieldsCorrectly()
		{
			Login();
			var bookFolder = MakeBook(MethodBase.GetCurrentMethod().Name, "myId", "me", "something");
			_uploader.UploadBook(bookFolder, new NullProgress());
			var bookInstanceId = "myId" + _thisTestId;
			var bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);

			// Verify new upload
			Assert.That(bookRecord.harvestState.Value, Is.EqualTo("New"));
			Assert.That(bookRecord.tags[0].Value, Is.EqualTo("system:Incoming"), "New books should always get the system:Incoming tag.");
			Assert.That(bookRecord.updateSource.Value.StartsWith("BloomDesktop "), Is.True, "updateSource should start with BloomDesktop when uploaded");
			Assert.That(bookRecord.updateSource.Value, Is.Not.EqualTo("BloomDesktop old"), "updateSource should not equal 'BloomDesktop old' when uploaded from current Bloom");
			DateTime lastUploadedDateTime = bookRecord.lastUploaded.iso.Value;
			var differenceBetweenNowAndCreationOfJson = DateTime.UtcNow - lastUploadedDateTime;
			Assert.That(differenceBetweenNowAndCreationOfJson, Is.GreaterThan(TimeSpan.FromSeconds(0)), "lastUploaded should be a valid date representing now-ish");
			Assert.That(differenceBetweenNowAndCreationOfJson, Is.LessThan(TimeSpan.FromSeconds(5)), "lastUploaded should be a valid date representing now-ish");

			// Set up for re-upload
			_parseClient.SetBookRecord(JsonConvert.SerializeObject(
				new
				{
					bookInstanceId,
					updateSource = "not Bloom",
					tags = new string[0],
					lastUploaded = (ParseServerDate)null,
					harvestState = "Done"
				}));
			bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);
			Assert.That(bookRecord.harvestState.Value, Is.EqualTo("Done"));
			Assert.That(bookRecord.tags, Is.Empty);
			Assert.That(bookRecord.updateSource.Value, Is.EqualTo("not Bloom"));
			Assert.That(bookRecord.lastUploaded.Value, Is.Null);

			_uploader.UploadBook(bookFolder, new NullProgress());
			bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);

			// Verify re-upload
			Assert.That(bookRecord.harvestState.Value, Is.EqualTo("Updated"));
			Assert.That(bookRecord.tags[0].Value, Is.EqualTo("system:Incoming"), "Re-uploaded books should always get the system:Incoming tag.");
			Assert.That(bookRecord.updateSource.Value.StartsWith("BloomDesktop "), Is.True, "updateSource should start with BloomDesktop when re-uploaded");
			Assert.That(bookRecord.updateSource.Value, Is.Not.EqualTo("BloomDesktop old"), "updateSource should not equal 'BloomDesktop old' when uploaded from current Bloom");
			lastUploadedDateTime = bookRecord.lastUploaded.iso.Value;
			differenceBetweenNowAndCreationOfJson = DateTime.UtcNow - lastUploadedDateTime;
			Assert.That(differenceBetweenNowAndCreationOfJson, Is.GreaterThan(TimeSpan.FromSeconds(0)), "lastUploaded should be a valid date representing now-ish");
			Assert.That(differenceBetweenNowAndCreationOfJson, Is.LessThan(TimeSpan.FromSeconds(5)), "lastUploaded should be a valid date representing now-ish");

			_parseClient.DeleteBookRecord(bookRecord.objectId.Value);
		}

		// This is really testing parse server cloud code.
		// We are simulating uploading from an old Bloom where the updateSource and lastUploaded were not
		// sent from Bloom, but the cloud code sets them by recognizing we are coming from Bloom based
		// on the headers.user-agent being RestSharp.
		[Test]
		public void UploadBook_SimulateOldBloom_SetsInterestingParseFieldsCorrectly()
		{
			try
			{
				_parseClient.SimulateOldBloomUpload = true;

				Login();
				var bookFolder = MakeBook(MethodBase.GetCurrentMethod().Name, "myId", "me", "something");
				_uploader.UploadBook(bookFolder, new NullProgress());
				var bookInstanceId = "myId" + _thisTestId;
				var bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);

				// Verify new upload
				Assert.That(bookRecord.harvestState.Value, Is.EqualTo("New"));
				Assert.That(bookRecord.tags[0].Value, Is.EqualTo("system:Incoming"), "New books should always get the system:Incoming tag.");
				Assert.That(bookRecord.updateSource.Value, Is.EqualTo("BloomDesktop old"), "updateSource should start with BloomDesktop when uploaded");
				DateTime lastUploadedDateTime = bookRecord.lastUploaded.iso.Value;
				var differenceBetweenNowAndCreationOfJson = DateTime.UtcNow - lastUploadedDateTime;
				// Since we are actually comparing two different system clocks (parse server vs. machine running tests), we allow for 5 minutes either way.
				Assert.That(differenceBetweenNowAndCreationOfJson, Is.GreaterThan(TimeSpan.FromMinutes(-5)), "lastUploaded should be a valid date representing now-ish");
				Assert.That(differenceBetweenNowAndCreationOfJson, Is.LessThan(TimeSpan.FromMinutes(5)), "lastUploaded should be a valid date representing now-ish");

				// Set up for re-upload
				_parseClient.SimulateOldBloomUpload = false;
				_parseClient.SetBookRecord(JsonConvert.SerializeObject(
					new
					{
						bookInstanceId,
						updateSource = "not Bloom",
						tags = new string[0],
						lastUploaded = (ParseServerDate) null,
						harvestState = "Done"
					}));
				bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);
				Assert.That(bookRecord.harvestState.Value, Is.EqualTo("Done"));
				Assert.That(bookRecord.tags, Is.Empty);
				Assert.That(bookRecord.updateSource.Value, Is.EqualTo("not Bloom"));
				Assert.That(bookRecord.lastUploaded.Value, Is.Null);
				_parseClient.SimulateOldBloomUpload = true;

				_uploader.UploadBook(bookFolder, new NullProgress());
				bookRecord = _parseClient.GetSingleBookRecord(bookInstanceId);

				// Verify re-upload
				Assert.That(bookRecord.harvestState.Value, Is.EqualTo("Updated"));
				Assert.That(bookRecord.tags[0].Value, Is.EqualTo("system:Incoming"), "Re-uploaded books should always get the system:Incoming tag.");
				Assert.That(bookRecord.updateSource.Value, Is.EqualTo("BloomDesktop old"), "updateSource should start with BloomDesktop when re-uploaded");
				lastUploadedDateTime = bookRecord.lastUploaded.iso.Value;
				differenceBetweenNowAndCreationOfJson = DateTime.UtcNow - lastUploadedDateTime;
				// Since we are actually comparing two different system clocks (parse server vs. machine running tests), we allow for 5 minutes either way.
				Assert.That(differenceBetweenNowAndCreationOfJson, Is.GreaterThan(TimeSpan.FromMinutes(-5)), "lastUploaded should be a valid date representing now-ish");
				Assert.That(differenceBetweenNowAndCreationOfJson, Is.LessThan(TimeSpan.FromMinutes(5)), "lastUploaded should be a valid date representing now-ish");

				_parseClient.DeleteBookRecord(bookRecord.objectId.Value);
			}
			finally
			{
				_parseClient.SimulateOldBloomUpload = false;
			}
		}

		[Test]
		public void UploadBook_FillsInMetaData()
		{
			var bookFolder = MakeBook("My incomplete book", "", "", "data");
			File.WriteAllText(Path.Combine(bookFolder, "thumbnail.png"), @"this should be a binary picture");

			Login();
			string s3Id = _uploader.UploadBook(bookFolder, new NullProgress());
			_uploader.WaitUntilS3DataIsOnServer(BloomS3Client.UnitTestBucketName, bookFolder);
			var dest = _workFolderPath.CombineForPath("output");
			Directory.CreateDirectory(dest);
			var newBookFolder = _downloader.DownloadBook(BloomS3Client.UnitTestBucketName, s3Id, dest);
			var metadata = BookMetaData.FromString(File.ReadAllText(Path.Combine(newBookFolder, BookInfo.MetaDataFileName)));
			Assert.That(string.IsNullOrEmpty(metadata.Id), Is.False, "should have filled in missing ID");
			Assert.That(metadata.Uploader.ObjectId, Is.EqualTo(_parseClient.UserId), "should have set uploader to id of logged-in user");
			Assert.That(metadata.DownloadSource, Is.EqualTo(s3Id));

			var record = _parseClient.GetSingleBookRecord(metadata.Id);
			string baseUrl = record.baseUrl;
			Assert.That(baseUrl.StartsWith("https://s3.amazonaws.com/BloomLibraryBooks"), "baseUrl should start with s3 prefix");

			string order = record.bookOrder;
			Assert.That(order, Does.Contain("My+incomplete+book.BloomBookOrder"), "order url should include correct file name");
			Assert.That(order.StartsWith(BloomLinkArgs.kBloomUrlPrefix + BloomLinkArgs.kOrderFile + "="), "order url should start with Bloom URL prefix");

			Assert.That(File.Exists(Path.Combine(newBookFolder, "My incomplete book.BloomBookOrder")), "Should have created, uploaded and downloaded the book order");
		}

		[Test]
		public void DownloadUrl_GetsDocument()
		{
			var id = Guid.NewGuid().ToString();
			var bookFolder = MakeBook("My Url Book", id, "someone", "My content");
			int fileCount = Directory.GetFiles(bookFolder).Length;
			Login();
			string s3Id = _uploader.UploadBook(bookFolder, new NullProgress());
			_uploader.WaitUntilS3DataIsOnServer(BloomS3Client.UnitTestBucketName, bookFolder);
			var dest = _workFolderPath.CombineForPath("output");
			Directory.CreateDirectory(dest);

			var newBookFolder = _downloader.DownloadFromOrderUrl(_uploader.BookOrderUrlOfLastUploadForUnitTest, dest, "nonsense");
			Assert.That(Directory.GetFiles(newBookFolder).Length, Is.EqualTo(fileCount + 1)); // book order is added during upload
		}

		[Test]
		public void SmokeTest_DownloadKnownBookFromProductionSite()
		{
			var dest = _workFolderPath.CombineForPath("output");
			Directory.CreateDirectory(dest);

			//if this fails, don't panic... maybe the book is gone. If so, just pick another one.
			var url =
			"bloom://localhost/order?orderFile=BloomLibraryBooks/cara_ediger%40sil-lead.org%2ff0665264-4f1f-43d3-aa7e-fc832fe45dd0%2fBreakfast%2fBreakfast.BloomBookOrder&title=Breakfast";
			var destBookFolder = _downloader.DownloadFromOrderUrl(url, dest, "nonsense");
			Assert.That(Directory.GetFiles(destBookFolder).Length, Is.GreaterThan(3));
		}

		/// <summary>
		/// This test has a possible race condition which we attempted to fix by setting ClassesLanguagePath to something unique
		/// in the BloomParseClientDouble constructor. This had a disastrous side effect documented there. Disable until we find
		/// a better way.
		/// </summary>
		[Test, Ignore("This test in its current form can fail when multiple clients are running tests")]
		public void GetLanguagePointers_CreatesLanguagesButDoesNotDuplicate()
		{
			Login();
			try
			{
				_parseClient.CreateLanguage(new LanguageDescriptor() {IsoCode = "en", Name="English", EthnologueCode = "eng"});
				_parseClient.CreateLanguage(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyLang", EthnologueCode = "xyk" });
				Assert.That(_parseClient.LanguageExists(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyLang", EthnologueCode = "xyk" }));
				Assert.That(_parseClient.LanguageExists(new LanguageDescriptor() { IsoCode = "xyj", Name = "MyLang", EthnologueCode = "xyk" }), Is.False);
				Assert.That(_parseClient.LanguageExists(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyOtherLang", EthnologueCode = "xyk" }), Is.False);
				Assert.That(_parseClient.LanguageExists(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyLang", EthnologueCode = "xyj" }), Is.False);

				var pointers = _parseClient.GetLanguagePointers(new[]
				{
					new LanguageDescriptor() {IsoCode = "xyk", Name = "MyLang", EthnologueCode = "xyk"},
					new LanguageDescriptor() {IsoCode = "xyk", Name = "MyOtherLang", EthnologueCode = "xyk"}
				});
				Assert.That(_parseClient.LanguageExists(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyOtherLang", EthnologueCode = "xyk" }));
				Assert.That(_parseClient.LanguageCount(new LanguageDescriptor() { IsoCode = "xyk", Name = "MyLang", EthnologueCode = "xyk" }), Is.EqualTo(1));

				Assert.That(pointers[0], Is.Not.Null);
				Assert.That(pointers[0].ClassName, Is.EqualTo("language"));
				var first = _parseClient.GetLanguage(pointers[0].ObjectId);
				Assert.That(first.name.Value, Is.EqualTo("MyLang"));
				var second = _parseClient.GetLanguage(pointers[1].ObjectId);
				Assert.That(second.name.Value, Is.EqualTo("MyOtherLang"));
			}
			finally
			{
				_parseClient.DeleteLanguages();
			}
		}
	}
}
