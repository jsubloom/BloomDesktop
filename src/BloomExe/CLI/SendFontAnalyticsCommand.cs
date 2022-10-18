﻿using System;
using System.Threading;
using CommandLine;
using L10NSharp;
using Bloom.Properties;
using Bloom.Book;
using BloomTemp;
using Bloom.Publish.Android;
using Bloom.ToPalaso;

namespace Bloom.CLI
{
	[Flags]
	enum SendFontAnalyticsExitCode
	{
		// These flags should all be powers of 2, so they can be bit-or'd together
		// Make sure to update GetErrorsFromExitCode() too
		Success = 0,
		UnhandledException = 1,
		NoFontsFound = 2,
	}

	public class SendFontAnalyticsCommand
	{
		static ProjectContext _projectContext;
		static string _bookID;

		public static int Handle(SendFontAnalyticsParameters options)
		{
			Program.SetUpErrorHandling();
			try
			{
				using (var applicationContainer = new ApplicationContainer())
				{
					Program.SetUpLocalization(applicationContainer);
					GeckoFxBrowser.SetUpXulRunner();
					LocalizationManager.SetUILanguage(Settings.Default.UserInterfaceLanguage, false);   // Unclear if this line is needed or not.
					Program.RunningHarvesterMode = true;
					using (_projectContext = applicationContainer.CreateProjectContext(options.CollectionPath))
					{
						Program.SetProjectContext(_projectContext);
						CollectFontAnalytics(options);
						if (BloomPubMaker.BloomPubFontsAndLangsUsed != null)
						{
							// Report what we can about fonts and languages for this book.
							// (See https://issues.bloomlibrary.org/youtrack/issue/BL-11512.)
							ReportFontAnalytics(_bookID, "harvester sendFontAnalytics", options.Testing);
							return (int)SendFontAnalyticsExitCode.Success;
						}
					}
					return (int)SendFontAnalyticsExitCode.NoFontsFound;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				return (int)SendFontAnalyticsExitCode.UnhandledException;
			}
		}

		/// <summary>
		/// Report font analytics from a command line used by the harvester.
		/// </summary>
		/// <param name="bookId">book's guid</param>
		/// <param name="details">something like "harvester createArtifacts" or "harvester sendFontAnalytics"</param>
		/// <param name="forTesting">not from the production site, so can be ignored by real users</param>
		/// <remarks>
		/// See https://issues.bloomlibrary.org/youtrack/issue/BL-11512 for original specification.
		/// </remarks>
		public static void ReportFontAnalytics(string bookId, string details, bool forTesting)
		{
			var testOnly = forTesting || WebLibraryIntegration.BookUpload.UseSandboxByDefault;
			foreach (var fontName in BloomPubMaker.BloomPubFontsAndLangsUsed.Keys)
			{
				foreach (var lang in BloomPubMaker.BloomPubFontsAndLangsUsed[fontName])
				{
					FontAnalytics.Report("Bloom Library", "2.0", bookId,
						FontAnalytics.FontEventType.PublishEbook, lang, testOnly, fontName, details);
					FontAnalytics.Report("Bloom Library", "2.0", bookId,
						FontAnalytics.FontEventType.PublishWeb, lang, testOnly, fontName, details);
					FontAnalytics.Report("Bloom Library", "2.0", bookId,
						FontAnalytics.FontEventType.PublishPdf, lang, testOnly, fontName, details);
				}
			}
			// If this method quits before all the reports have actually been sent, then the
			// program exits without some reports being sent.  Squeezing all of this processing
			// into the async/await paradigm without passing that back up to Main() is not worth
			// the code intricacy and programmer time to get it working.  (Believe me, I tried.)
			// Waiting on the flag is the simplest thing that can work, and it does.
			while (FontAnalytics.PendingReports)
			{
				// The timer used by FontAnalytics.Report() to handle queued requests runs on
				// its own thread, so sleeping on this thread works okay.
				Thread.Sleep(1000);
			}
		}

		private static void CollectFontAnalytics(SendFontAnalyticsParameters options)
		{
			using (var stagingFolder = new TemporaryFolder("FontAnalytics"))
			{
				var bookServer = _projectContext.BookServer;
				var metadata = BookMetaData.FromFolder(options.BookPath);
				bool isTemplateBook = metadata.IsSuitableForMakingShells;
				_bookID = metadata.Id;
				var bookInfo = new BookInfo(options.BookPath, false);
				var settings = AndroidPublishSettings.GetPublishSettingsForBook(bookServer, bookInfo);
				// This method will gather up the desired font analytics as a side-effect.
				BloomPubMaker.PrepareBookForBloomReader(options.BookPath, bookServer, stagingFolder,
					new Bloom.web.NullWebSocketProgress(), isTemplateBook, settings: settings);
			}
		}
	}

	[Verb("sendFontAnalytics", HelpText ="Send font analytics for the given book.")]
	public class SendFontAnalyticsParameters
	{
		[Value(0, MetaName = "bookPath", Required = true, HelpText = "Path to the folder of the book to get font analytics from.")]
		public string BookPath { get; set; }

		[Option("collectionPath", Required = true, HelpText = "Input path of the Bloom collection file for this book")]
		public string CollectionPath { get; set; }

		[Option("testing", Required = false, Default = false, HelpText = "Analytics are being sent for testing, not production")]
		public bool Testing { get; set; }
	}
}
