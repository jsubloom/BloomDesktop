using System;
using Bloom.Book;
using Bloom.Spreadsheet;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Bloom.web;
using BloomTemp;
using BloomTests.TeamCollection;
using BloomTests.web;
using Pango;
using SIL.IO;

namespace BloomTests.Spreadsheet
{
	/// <summary>
	/// Class tests using spreadsheet IO to change the language of a set of blocks.
	/// Also the content of a couple of other elements is changed.
	/// </summary>
	public class SpreadsheetImporterChangeLanguageTests
	{
		private InternalSpreadsheet _sheet;
		protected HtmlDom _dom;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_dom = new HtmlDom(SpreadsheetTests.kSimpleTwoPageBook, true);
			AssertThatXmlIn.Dom(_dom.RawDom).HasSpecifiedNumberOfMatchesForXpath("//div[@lang='en']/p[text()='Riding on elephants can be risky.']", 1); // unchanged

			// The tests in this class all check the results of importing what export produced,
			// but with some changes.

			var mockLangDisplayNameResolver = new Mock<ILanguageDisplayNameResolver>();
			mockLangDisplayNameResolver.Setup(x => x.GetLanguageDisplayName("en")).Returns("English");
			mockLangDisplayNameResolver.Setup(x => x.GetLanguageDisplayName("de")).Returns("German");
			mockLangDisplayNameResolver.Setup(x => x.GetLanguageDisplayName("fr")).Returns("French");
			mockLangDisplayNameResolver.Setup(x => x.GetLanguageDisplayName("tpi")).Returns("Tok Pisin");
			var exporter = new SpreadsheetExporter(mockLangDisplayNameResolver.Object);
			_sheet = exporter.Export(_dom, "fakeImagesFolderpath");
			// Changing this header will cause all the data that was originally tagged as German to be imported as Tok Pisin.
			var indexDe = _sheet.GetRequiredColumnForLang("de");
			_sheet.Header.SetColumn(indexDe, "[tpi]", "Tok Pisin");
			// Here we locate the cells produced from two particular
			// bloom-editable elements in the kSimpleTwoPageBook DOM and replace them with different text.
			// The import should update those bloom-editables to these changed values.
			var engColumn = _sheet.GetRequiredColumnForLang("en");
			var firstRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.GetCell(engColumn).Content.Contains("This elephant is running amok."));
			Assert.IsNotNull(firstRowToModify, "Did not find the first text row that OneTimeSetup was expecting to modify");
			firstRowToModify.SetCell(engColumn, "<p>This elephant is running amok.</p>");
			var frColumn = _sheet.GetRequiredColumnForLang("fr");
			var secondRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.GetCell(frColumn).Content.Contains("Riding on French elephants can be more risky."));
			Assert.IsNotNull(secondRowToModify, "Did not find the second text row that OneTimeSetup was expecting to modify");
			secondRowToModify.SetCell(frColumn, "<p>Riding on French elephants can be very risky.</p>");

			var asteriskColumn = _sheet.GetRequiredColumnForLang("*");
			var imageSrcColumn = _sheet.GetColumnForTag(InternalSpreadsheet.ImageSourceColumnLabel);

			var firstXmatterRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.MetadataKey.Contains("styleNumberSequence"));
			Assert.IsNotNull(firstXmatterRowToModify, "Did not find the first xmatter row that OneTimeSetup was expecting to modify");
			firstXmatterRowToModify.SetCell(asteriskColumn, "7");

			var secondXmatterRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.MetadataKey.Contains("coverImage"));
			Assert.IsNotNull(secondXmatterRowToModify, "Did not find the second xmatter row that OneTimeSetup was expecting to modify");
			secondXmatterRowToModify.SetCell(imageSrcColumn, "octopus.png");

			var thirdXmatterRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.MetadataKey.Contains("bookTitle"));
			Assert.IsNotNull(thirdXmatterRowToModify, "Did not find the third xmatter row that OneTimeSetup was expecting to modify");
			thirdXmatterRowToModify.SetCell(_sheet.GetRequiredColumnForLang("fr"), "");
			thirdXmatterRowToModify.SetCell(_sheet.GetRequiredColumnForLang("tpi"), "");
			thirdXmatterRowToModify.SetCell(_sheet.GetRequiredColumnForLang("en"), "<p>This is Not the End of the English World</p>");

			var fourthXmatterRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.MetadataKey.Contains("contentLanguage1"));
			fourthXmatterRowToModify.SetCell(_sheet.GetColumnForTag(InternalSpreadsheet.RowTypeColumnLabel), "[newDataBookLabel]");
			fourthXmatterRowToModify.SetCell(asteriskColumn, "newContent");

			// This was for testing the special behavior for DataDivImagesWithNoSrcAttributes. However, the only such element
			// is licenseImage, and we now don't export that at all.
			//var fifthXmatterRowToModify = _sheet.ContentRows.FirstOrDefault(row => row.MetadataKey.Contains("licenseImage"));
			//Assert.IsNotNull(fifthXmatterRowToModify, "Did not find the fifth xmatter row that OneTimeSetup was expecting to modify");
			//fifthXmatterRowToModify.SetCell(imageSrcColumn, "newLicenseImage.png");

			var importer = new SpreadsheetImporter(null, this._dom);
			InitializeImporter(importer);
			importer.Import(_sheet);
		}

		/// <summary>
		/// Provides a hook where a subclass can change something to apply similar tests with
		/// a slightly different state (e.g., different import params).
		/// </summary>
		protected virtual void InitializeImporter(SpreadsheetImporter importer)
		{
		}

		[Test]
		public void TokPisinAdded()
		{
			var assertDom = AssertThatXmlIn.Dom(_dom.RawDom);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class, 'bloom-editable') and @lang='tpi']", 2);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@lang='tpi']/p[text()='German elephants are quite orderly.']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@lang='tpi']/p[text()='Riding on German elephants can be less risky.']", 1);
		}

		[Test]
		public virtual void GermanKept()
		{
			var assertDom = AssertThatXmlIn.Dom(_dom.RawDom);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class, 'bloom-editable') and @lang='de' and not(@data-book)]", 2);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@lang='de']/p[text()='German elephants are quite orderly.']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@lang='de']/p[text()='Riding on German elephants can be less risky.']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='de']", 1);
		}

		[Test]
		public void EnglishAndFrenchKeptOrModified()
		{
			var assertDom = AssertThatXmlIn.Dom(_dom.RawDom);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class, 'bloom-translationGroup') and not(contains(@class, 'box-header-off'))]/div[contains(@class, 'bloom-editable') and @lang='en' and not(@data-book='bookTitle')]", 5);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[contains(@class, 'bloom-editable') and @lang='fr' and not(@data-book='bookTitle')]", 3);
			// Make sure these are in the right places. We put this in the first row, which because of tab index should be imported to the SECOND TG.
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@tabindex='1']/div[@lang='en']/p[text()='This elephant is running amok.']", 1); // modified
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='group3']/div[@lang='fr']/p[text()='Riding on French elephants can be very risky.']", 1); // modified
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='group3']/div[@lang='en']/p[text()='Riding on elephants can be risky.']", 1); // unchanged
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@tabindex='2']/div[@lang='fr']/p[text()='French elephants should be handled with special care.']", 1); // unchanged
		}

		[Test]
		public void XmatterChanged()
		{
			var assertDom = AssertThatXmlIn.Dom(_dom.RawDom);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='styleNumberSequence' and @lang='*' and text()='7']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='styleNumberSequence' and text()='1']", 0);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='coverImage' and @src='octopus.png' and text()='octopus.png']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='coverImage']", 1); // should have got rid of or updated the old one
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='fr']", 0); 
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='tpi']", 0);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='bookTitle' and @lang='en']/p[text()='This is Not the End of the English World']", 1);

			//We changed contentLanguage1 to newDataBookLabel. The importer should keep its old contentLanguage1 element, since there is no input for it,
			//as well as adding a new element for newDataBookLabel
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='contentLanguage1' and @lang='*']", 1);
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='newDataBookLabel' and @lang='*' and text()='newContent']", 1);

			// For testing DataDivImagesWithNoSrcAttributes. But we no longer export licenseImg at all.
			//assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@id='bloomDataDiv']/div[@data-book='licenseImage' and @lang='*' and not(@src) and text()='newLicenseImage.png']", 1);

			//make sure z language node is not removed
			assertDom.HasSpecifiedNumberOfMatchesForXpath("//div[@data-book='ztest' and @lang='z']", 1);
		}
	}

	/// <summary>
	/// Test the alternative behaviour where languages not in the spreadsheet are cleaned out.
	/// </summary>
	public class SpreadsheetImporterChangeLanguageCleanTests : SpreadsheetImporterChangeLanguageTests
	{
		protected override void InitializeImporter(SpreadsheetImporter importer)
		{
			base.InitializeImporter(importer);
			importer.Params = new SpreadsheetImportParams() {RemoveOtherLanguages = true};
		}

		// In this test subclass, which runs the import with RemoveOtherLanguages true, German should be removed.
		// We override the GermanKept test and do NOT mark it as a test so it will not be included in
		// the test cases for this subclass.
		public override void GermanKept()
		{
		}

		[Test]
		public void GermanRemoved()
		{
			var assertDom = AssertThatXmlIn.Dom(_dom.RawDom);
			assertDom.HasNoMatchForXpath("//div[contains(@class, 'bloom-editable') and @lang='de']");
			assertDom.HasNoMatchForXpath("//div[@data-book and @lang='de']");
		}
	}

	// Enhance: no test currently checks what happens when we have imported all the spreadsheet
	// content but the book still has more pages (or more blocks on the last page).
	// Currently the behavior is to leave the later pages and blocks unchanged.
	// Possibly we should clear their content or remove the pages altogether.

	public class SpreadsheetImageAndTextImportTests
	{
		private HtmlDom _dom;

		private TemporaryFolder _bookFolder;
		private TemporaryFolder _otherImagesFolder;
		private ProgressSpy _progressSpy;

		public static string PageWithImageAndText(int pageNumber, int tgNumber, int icNumber, string editableDivs = "")
		{
			return string.Format(@"	<div class=""bloom-page numberedPage customPage bloom-combinedPage A5Portrait side-right bloom-monolingual"" data-page="""" id=""dc90dbe0-7584-4d9f-bc06-0e0326060054"" data-pagelineage=""adcd48df-e9ab-4a07-afd4-6a24d0398382"" data-page-number=""{0}"" lang="""">
        <div class=""pageLabel"" data-i18n=""TemplateBooks.PageLabel.Basic Text &amp; Picture"" lang=""en"">
            Basic Text &amp; Picture
        </div>

        <div class=""pageDescription"" lang=""en""></div>

        <div class=""split-pane-component marginBox"" style="""">
            <div class=""split-pane horizontal-percent"" style=""min-height: 42px;"">
                <div class=""split-pane-component position-top"">
                    <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                        <div class=""bloom-translationGroup bloom-trailingElement"" data-default-languages=""auto"" data-test-id=""tg{1}"">
                           <div class=""bloom-editable normal-style"" style="""" lang=""z"" contenteditable=""true"">
                                <p></p>
                            </div>
							{3}
                        </div>
                    </div>
                </div>
            </div>
			<div class=""split-pane-component position-top"">
                <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                    <div class=""bloom-imageContainer bloom-leadingElement"" data-test-id=""ic{2}""><img src=""placeHolder.png"" alt="""" data-copyright="""" data-creator="""" data-license=""""></img></div>
                </div>
            </div>
        </div>
    </div>", pageNumber, tgNumber, icNumber, editableDivs);
		}

		public static string PageWith2ImagesAnd2Texts(int pageNumber, int tgNumber, int icNumber)
		{
			return string.Format(@"	<div class=""bloom-page numberedPage customPage bloom-combinedPage A5Portrait side-right bloom-monolingual"" data-page="""" id=""dc90dbe0-7584-4d9f-bc06-0e0326060054"" data-pagelineage=""adcd48df-e9ab-4a07-afd4-6a24d0398382"" data-page-number=""{0}"" lang="""">
        <div class=""pageLabel"" data-i18n=""TemplateBooks.PageLabel.Basic Text &amp; Picture"" lang=""en"">
            Basic Text &amp; Picture
        </div>

        <div class=""pageDescription"" lang=""en""></div>

        <div class=""split-pane-component marginBox"" style="""">
            <div class=""split-pane horizontal-percent"" style=""min-height: 42px;"">
                <div class=""split-pane-component position-top"">
                    <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
						<div class=""box-header-off bloom-translationGroup bloom-trailingElement"" data-default-languages=""auto"">
                           <div class=""bloom-editable normal-style"" style="""" lang=""z"" contenteditable=""true"">
                                <p></p>
                            </div>
							<div class=""bloom-editable normal-style"" style="""" lang=""en"" contenteditable=""true"">
                                <p></p>
                            </div>
                        </div>
                        <div class=""bloom-translationGroup bloom-trailingElement"" data-default-languages=""auto"" data-test-id=""tg{1}"">
                           <div class=""bloom-editable normal-style"" style="""" lang=""z"" contenteditable=""true"">
                                <p></p>
                            </div>
							<div class=""bloom-editable normal-style"" style="""" lang=""en"" contenteditable=""true"">
                                <p>English group 1 from the source template page</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
			<div class=""split-pane-component position-top"">
                <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                    <div class=""bloom-imageContainer bloom-leadingElement"" data-test-id=""ic{2}""><img src=""Othello 199.jpg"" alt="""" data-copyright="""" data-creator="""" data-license=""""></img></div>
                </div>
            </div>
			<div class=""split-pane horizontal-percent"" style=""min-height: 42px;"">
                <div class=""split-pane-component position-top"">
                    <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                        <div class=""bloom-translationGroup bloom-trailingElement"" data-default-languages=""auto"" data-test-id=""tg{3}"">
                           <div class=""bloom-editable normal-style"" style="""" lang=""z"" contenteditable=""true"">
                                <p></p>
                            </div>
							<div class=""bloom-editable normal-style"" style="""" lang=""en"" contenteditable=""true"">
                                <p>English group 2 from the source template page</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
			<div class=""split-pane-component position-top"">
                <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                    <div class=""bloom-imageContainer bloom-leadingElement"" data-test-id=""ic{4}""><img src=""Othello 199.jpg"" alt="""" data-copyright="""" data-creator="""" data-license=""""></img></div>
                </div>
            </div>
        </div>
    </div>
", pageNumber, tgNumber, icNumber, tgNumber+1, icNumber+1);
		}



		public static string PageWithJustText(int pageNumber, int tgNumber)
		{
			return string.Format(@"	<div class=""bloom-page numberedPage customPage bloom-combinedPage A5Portrait side-right bloom-monolingual"" data-page="""" id=""dc90dbe0-7584-4d9f-bc06-0e0326060054"" data-pagelineage=""adcd48df-e9ab-4a07-afd4-6a24d0398382"" data-page-number=""{0}"" lang="""">
        <div class=""pageLabel"" data-i18n=""TemplateBooks.PageLabel.Basic Text &amp; Picture"" lang=""en"">
            Basic Text &amp; Picture
        </div>

        <div class=""pageDescription"" lang=""en""></div>

        <div class=""split-pane-component marginBox"" style="""">
            <div class=""split-pane horizontal-percent"" style=""min-height: 42px;"">
                <div class=""split-pane-component position-top"">
                    <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                        <div class=""bloom-translationGroup bloom-trailingElement"" data-default-languages=""auto"" data-test-id=""tg{1}"">
                           <div class=""bloom-editable normal-style"" style="""" lang=""z"" contenteditable=""true"">
                                <p></p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>", pageNumber, tgNumber);
		}

		// The image has a slot for an imageDescription, which will mess up all kinds of things
		// if we don't ignore it properly.
		public static string PageWithJustImage(int pageNumber, int icNumber)
		{
			return string.Format(@"	<div class=""bloom-page numberedPage customPage bloom-combinedPage A5Portrait side-right bloom-monolingual"" data-page="""" id=""dc90dbe0-7584-4d9f-bc06-0e0326060054"" data-pagelineage=""adcd48df-e9ab-4a07-afd4-6a24d0398382"" data-page-number=""{0}"" lang="""">
        <div class=""pageLabel"" data-i18n=""TemplateBooks.PageLabel.Basic Text &amp; Picture"" lang=""en"">
            Basic Text &amp; Picture
        </div>

        <div class=""pageDescription"" lang=""en""></div>

        <div class=""split-pane-component marginBox"" style="""">
			<div class=""split-pane-component position-top"">
                <div class=""split-pane-component-inner"" min-width=""60px 150px 250px"" min-height=""60px 150px 250px"">
                    <div class=""bloom-imageContainer bloom-leadingElement"" title=""this might be nonsense after import"" data-test-id=""ic{1}"">
						<img src=""placeholder.png"" alt="""" data-copyright="""" data-creator="""" data-license="""" height=""100"" width=""200""></img>
						<div class=""bloom-translationGroup bloom-imageDescription bloom-trailingElement"" data-default-languages=""auto"">
		                    <div class=""bloom-editable ImageDescriptionEdit-style"" lang=""z"" contenteditable=""true"" data-book=""coverImageDescription""></div>
		                </div>
					</div>
                </div>
            </div>
        </div>
    </div>", pageNumber, icNumber);
		}

		public static string coverPage =
			@"<div class=""bloom-page cover coverColor bloom-frontMatter frontCover outsideFrontCover A5Portrait side-right"" data-page=""required singleton"" data-export=""front-matter-cover"" id=""bc85989e-9503-4f4e-b12f-f83a4002937f"">
        <div class=""pageLabel"" lang=""en"">
            Front Cover
        </div>
        <div class=""pageDescription"" lang=""en""></div>

        <div class=""marginBox"">
            <div class=""bloom-translationGroup bookTitle"" data-default-languages=""V,N1"">
                <label class=""bubble"">Book title in {lang}</label>
                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style"" lang=""z"" contenteditable=""true"" data-book=""bookTitle""></div>
                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style bloom-contentNational2"" lang=""de"" contenteditable=""true"" data-book=""bookTitle""></div>
                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style bloom-contentNational1 bloom-visibility-code-on"" lang=""ar"" contenteditable=""true"" data-book=""bookTitle"" dir=""rtl""></div>

                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style bloom-content1 bloom-visibility-code-on"" lang=""ksf"" contenteditable=""true"" data-book=""bookTitle"">
                    <p>a lion book</p>
                </div>

                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style"" lang=""en"" contenteditable=""true"" data-book=""bookTitle"">
                    <p>Another lion book</p>
                </div>
                <div class=""bloom-editable bloom-nodefaultstylerule Title-On-Cover-style"" lang=""*"" contenteditable=""true"" data-book=""bookTitle""></div>
            </div>
            <div class=""bloom-imageContainer bloom-backgroundImage"" data-book=""coverImage"" style=""background-image:url('AOR_alb010.png')"" data-copyright=""Copyright, SIL International 2009."" data-creator="""" data-license=""cc-by-nd""></div>

            <div class=""bottomBlock"">
                <img class=""branding"" src=""/bloom/api/branding/image?id=cover-bottom-left.svg"" type=""image/svg"" onerror=""this.style.display='none'""></img> 

                <div class=""bottomTextContent"">
                    <div class=""creditsRow"" data-hint=""You may use this space for author/illustrator, or anything else."">
                        <div class=""bloom-translationGroup"" data-default-languages=""V"">
                            <div class=""bloom-editable Cover-Default-style"" lang=""z"" contenteditable=""true"" data-book=""smallCoverCredits""></div>
                            <div class=""bloom-editable Cover-Default-style bloom-contentNational2"" lang=""de"" contenteditable=""true"" data-book=""smallCoverCredits""></div>
                            <div class=""bloom-editable Cover-Default-style bloom-contentNational1"" lang=""ar"" contenteditable=""true"" data-book=""smallCoverCredits"" dir=""rtl""></div>
                            <div class=""bloom-editable Cover-Default-style bloom-content1 bloom-visibility-code-on"" lang=""ksf"" contenteditable=""true"" data-book=""smallCoverCredits""></div>
                        </div>
                    </div>

                    <div class=""bottomRow"" data-have-topic=""false"">
                        <div class=""coverBottomLangName Cover-Default-style"" data-book=""languagesOfBook"">
                            Bafia
                        </div>

                        <div class=""coverBottomBookTopic bloom-userCannotModifyStyles bloom-alwaysShowBubble Cover-Default-style"" data-derived=""topic"" data-functiononhintclick=""ShowTopicChooser()"" data-hint=""Click to choose topic""></div>
                    </div>
                </div>
            </div>
        </div>
    </div>";

		public static string insideBackCoverPage = @"    <div class=""bloom-page cover coverColor insideBackCover bloom-backMatter A5Portrait layout-style-Default side-left"" data-page=""required singleton"" data-export=""back-matter-inside-back-cover"" id=""9ab4856a-c292-43b3-b430-10dd37cdeaf2"" data-page-number=""9"">
        <div class=""pageLabel"" lang=""en"">
            Inside Back Cover
        </div>
        <div class=""pageDescription"" lang=""en""></div>

        <div class=""marginBox"">
            <div class=""bloom-translationGroup"" data-default-languages=""N1"">
                <div class=""bloom-editable Inside-Back-Cover-style bloom-content2 bloom-contentNational1 bloom-visibility-code-on"" lang=""en"" contenteditable=""true"" data-book=""insideBackCover"" dir=""rtl"">
                    <label class=""bubble"">If you need somewhere to put more information about the book, you can use this page, which is the inside of the back cover.</label>
                </div>

                <div class=""bloom-editable Inside-Back-Cover-style bloom-content3 bloom-contentNational2"" lang=""de"" contenteditable=""true"" data-book=""insideBackCover""></div>

                <div class=""bloom-editable Inside-Back-Cover-style bloom-content1"" lang=""ksf"" contenteditable=""true"" data-book=""insideBackCover""></div>
            </div>
        </div>
    </div>";

		public static string backCoverPage =
			@"    <div class=""bloom-page cover coverColor outsideBackCover bloom-backMatter A5Portrait side-right"" data-page=""required singleton"" data-export=""back-matter-back-cover"" id=""ec266f7c-61b1-4c08-b5a3-cce44544e900"">
        <div class=""pageLabel"" lang=""en"">
            Outside Back Cover
        </div>
        <div class=""pageDescription"" lang=""en""></div>

        <div class=""marginBox"">
        <div class=""bloom-translationGroup"" data-default-languages=""N1"">
            <div class=""bloom-editable Outside-Back-Cover-style bloom-contentNational1 bloom-visibility-code-on"" lang=""ar"" contenteditable=""true"" data-book=""outsideBackCover"" dir=""rtl"">
                <label class=""bubble"">If you need somewhere to put more information about the book, you can use this page, which is the outside of the back cover.</label>
            </div>

            <div class=""bloom-editable Outside-Back-Cover-style bloom-contentNational2"" lang=""de"" contenteditable=""true"" data-book=""outsideBackCover""></div>

            <div class=""bloom-editable Outside-Back-Cover-style bloom-content1"" lang=""ksf"" contenteditable=""true"" data-book=""outsideBackCover""></div>
        </div><img class=""branding branding-wide"" src=""/bloom/api/branding/image?id=back-cover-outside-wide.svg"" type=""image/svg"" onerror=""this.style.display='none'""></img> <img class=""branding"" src=""/bloom/api/branding/image?id=back-cover-outside.svg"" type=""image/svg"" onerror=""this.style.display='none'""></img></div>
    </div>";

		public static string templateDom = @"
<!DOCTYPE html>

<html>
<head>
</head>

<body data-l1=""es"" data-l2="""" data-l3="""">
	<div id=""bloomDataDiv"">
		<div data-book=""bookTitle"" lang=""en"" id=""idShouldGetKept"">
			<p><em>Pineapple</em></p>

            <p>Farm</p>

		</div>
        <div data-book=""topic"" lang=""en"">
            Health
		</div>
		<div data-book=""coverImage"" lang=""*"" src=""cover.png"" alt=""This picture, placeHolder.png, is missing or was loading too slowly."">
			cover.png
		</div>
		<div data-book=""licenseImage"" lang= ""*"" >
			license.png
		</div>
		<div data-book=""outside-back-cover-branding-bottom-html"" lang=""*""><img class=""branding"" src=""BloomWithTaglineAgainstLight.svg"" alt="""" data-copyright="""" data-creator="""" data-license=""""></img></div>
	</div>
	{0}
</body>
</html>
";

		private List<XmlElement> _contentPages;
		private XmlElement _firstPage;
		private XmlElement _lastPage;
		private XmlElement _secondLastPage;
		private List<string> _warnings;
		private string _spreadsheetFolder;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			// We will re-use the images from another test.
			// Conveniently the images are in a folder called "images" which is what the importer expects.
			// So we give it the parent directory of that images folder.
			_spreadsheetFolder = SIL.IO.FileLocationUtilities.GetDirectoryDistributedWithApplication("src/BloomTests/ImageProcessing");

			// A place to put an image that is not in the spreadsheet folder so we can test absolute path import.
			_otherImagesFolder = new TemporaryFolder("other images folder");

			// Create an HtmlDom for a template to import into
			var xml = string.Format(templateDom,
				coverPage
				+ PageWithJustText(1, 1)
				+ PageWithImageAndText(2, 2, 1,
					@"<div class=""bloom-editable normal-style"" style="""" lang=""en"" contenteditable=""true"">
                                <p>old English</p>
                            </div>")
				+ PageWithImageAndText(3, 3, 2)
				+ PageWithImageAndText(4, 4, 3)
				+ PageWithImageAndText(5, 5, 4)
				+ PageWith2ImagesAnd2Texts(6, 6, 5)
				+ PageWithJustText(7, 8)
				+ PageWithJustImage(8, 7)
				+ insideBackCoverPage
				+ backCoverPage);
			_dom = new HtmlDom(xml, true);

			// Set up the internal spreadsheet rows directly.
			var ss = new InternalSpreadsheet();
			var columnForEn = ss.AddColumnForLang("en", "English");
			var columnForImage = ss.GetColumnForTag(InternalSpreadsheet.ImageSourceColumnLabel);

			// Will fill tg1 on page 1
			var contentRow1 = new ContentRow(ss);
			contentRow1.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow1.SetCell(columnForEn, "this is page 1");

			// Will remove tg2 on page 2 (because we set the row to blank) and fill in ic1 on page 2
			var contentRow2 = new ContentRow(ss);
			contentRow2.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow2.SetCell(columnForEn, InternalSpreadsheet.BlankContentIndicator);
			contentRow2.SetCell(columnForImage, "images/lady24b.png");

			// Will fill tg3 on page 3
			var contentRow3 = new ContentRow(ss);
			contentRow3.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow3.SetCell(columnForEn, "this is page 3");

			// Will fill tg4 on page 4 and ic3 on page 4.
			// ic2 on page 3 should be skipped, so as to keep these two cells that are on the same
			// row in the spreadsheet on the same page in the book.
			var contentRow4 = new ContentRow(ss);
			contentRow4.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow4.SetCell(columnForEn, "this is page 4");
			contentRow4.SetCell(columnForImage, "images/missingBird.png");

			// Will fill ic4 on page 5
			var contentRow5 = new ContentRow(ss);
			contentRow5.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow5.SetCell(columnForImage, "images/man.png");

			// Will fill tg6 on page 6 and ic5 on page 6.
			// tg5 on page 5 should be skipped, again to keep things on the same row going to the same page.
			// tg6 should be made blank, and ic5 should be set to the placeholder
			var contentRow6 = new ContentRow(ss);
			contentRow6.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow6.SetCell(columnForEn, "<p>" + InternalSpreadsheet.BlankContentIndicator + "</p>");
			contentRow6.SetCell(columnForImage, InternalSpreadsheet.BlankContentIndicator);

			// Will fill tg7 on page 6 and ic6 on page 6
			var contentRow6a = new ContentRow(ss);
			contentRow6a.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow6a.SetCell(columnForEn, "this is page 6");
			contentRow6a.SetCell(columnForImage, "images/shirt.png");

			// Will cause a new page to be inserted, since the original 7th page is text-only
			// Tests finding file by full path
			var marsPath = Path.Combine(_spreadsheetFolder, "images/Mars 2.png");
			var getMarsPath = Path.Combine(_otherImagesFolder.FolderPath, "Mars 3.png");
			RobustFile.Copy(marsPath, getMarsPath, true);
			var contentRow7 = new ContentRow(ss);
			contentRow7.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow7.SetCell(columnForImage, getMarsPath);

			// Will cause a new page to be inserted, since the original 7th page is text-only
			var contentRow8 = new ContentRow(ss);
			contentRow8.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow8.SetCell(columnForEn, "this is something extra on a new page before 7");
			contentRow8.SetCell(columnForImage, "images/LakePendOreille.jpg");

			// This will (finally) fill tg8 on original page 7, now the 9th page
			var contentRow9 = new ContentRow(ss);
			contentRow9.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow9.SetCell(columnForEn, "this is page 9");

			// Since original page 8 is picture-only, this will cause a new text-only page to be inserted (tenth).
			var contentRow10 = new ContentRow(ss);
			contentRow10.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow10.SetCell(columnForEn, "This will go on a new page before 8");

			// Will also cause a new page to be inserted, since the original 8th page is image-only (11th)
			var contentRow11 = new ContentRow(ss);
			contentRow11.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow11.SetCell(columnForEn, "this is something extra on a new page before 8");
			contentRow11.SetCell(columnForImage, "images/levels.png");

			// Will finally fill the image on the original page 8, now page 12
			var contentRow12 = new ContentRow(ss);
			contentRow12.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow12.SetCell(columnForImage, getMarsPath);

			// Will also cause a new page to be inserted, since there are no more input pages.
			// Since the original final page can only hold text, we'll insert a basic image and text.
			var contentRow13 = new ContentRow(ss);
			contentRow13.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow13.SetCell(columnForEn, "this is something extra after all the original pages");
			contentRow13.SetCell(columnForImage, "images/man.png");

			// But this is a picture, so the extra page inserted can be a clone of the original page 8
			var contentRow14 = new ContentRow(ss);
			contentRow14.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow14.SetCell(columnForImage, "images/shirt.png");

			// We'll need another new page for this, not a clone since the last page template doesn't have a text slot.
			var contentRow15 = new ContentRow(ss);
			contentRow15.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow15.SetCell(columnForEn, "This will go on a new just-text page at the end");

			_bookFolder = new TemporaryFolder("SpreadsheetImageAndTextImportTests");

			// Do the import
			_progressSpy = new ProgressSpy();
			var importer = new SpreadsheetImporter(null, _dom, _spreadsheetFolder, _bookFolder.FolderPath);
			_warnings = importer.Import(ss, _progressSpy);

			_contentPages = _dom.SafeSelectNodes("//div[contains(@class, 'bloom-page')]").Cast<XmlElement>().ToList();

			// Remove the xmatter to get just the content pages, but save so we can test that too.
			_firstPage = _contentPages[0];
			_contentPages.RemoveAt(0);
			_lastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);
			_secondLastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);

			// (individual test methods will evaluate the result)
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_bookFolder?.Dispose();
			_otherImagesFolder.Dispose();
		}

		[TestCase(0, "tg1", "this is page 1")]
		[TestCase(2, "tg3", "this is page 3")]
		[TestCase(3, "tg4", "this is page 4")]
		[TestCase(5, "tg7", "this is page 6")]
		public void GotTextOnPageN(int n, string tag, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']/div[@lang='en' and text() = '{text}']", 1);
		}

		[TestCase(1, "tg2")]
		[TestCase(5, "tg6")]
		public void NoEnglishTextInBlock(int n, string tag)
		{
			// Sanity check: it does have the TG,
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']", 1);
			// but nothing was added to it.
			AssertThatXmlIn.Element(_contentPages[n]).HasNoMatchForXpath($".//div[@data-test-id='{tag}']/div[@lang='en']");
		}

		[Test]
		public void NoTitleAttributesRemainOnImageContainers()
		{
			AssertThatXmlIn.Dom(_dom.RawDom).HasNoMatchForXpath("//div[contains(@class, 'bloom-imageContainer') and @title]");
		}

		[Test]
		public void NoHeightOrWidthRemainOnImages()
		{
			AssertThatXmlIn.Dom(_dom.RawDom).HasNoMatchForXpath("//img[@height]");
			AssertThatXmlIn.Dom(_dom.RawDom).HasNoMatchForXpath("//img[@width]");
		}

		[TestCase(4)]
		public void NoTextAddedOnPageN(int n)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasNoMatchForXpath($".//div/div[@lang='en']");
		}

		[TestCase(8)]
		[TestCase(9)]
		public void NoImageContainerAddedOnPageN(int n)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasNoMatchForXpath($".//div[contains(@class, 'bloom-imageContainer')]");
		}

		[TestCase(1, "ic1", "lady24b.png")]
		[TestCase(2, "ic2", "placeHolder.png")] // not filled, because next row has both text and picture
		[TestCase(3, "ic3", "missingBird.png")]
		[TestCase(4, "ic4", "man.png")]
		[TestCase(5, "ic5", "placeHolder.png")] // Todo: should be placeholder
		[TestCase(5, "ic6", "shirt.png")]
		[TestCase(11, "ic7", "Mars%203.png")]
		[TestCase(13, "ic7", "shirt.png")]
		public void GotImageSourceOnPageN(int n, string tag, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']/img[@src='{text}']", 1);
		}

		// Since these pages are sourced from Basic Book, we can't use the data-test-id trick.
		[TestCase(6, "Mars%203.png")]
		public void PageAddedWithPicture(int n, string src)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-imageContainer')]/img[@src='{src}']", 1);
			// This is the ID for the standard "Just a picture" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("adcd48df-e9ab-4a07-afd4-6a24d0398385"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("adcd48df-e9ab-4a07-afd4-6a24d0398385"));
		}

		[TestCase(7, "this is something extra on a new page before 7", "LakePendOreille.jpg", "Copyright © 2012, Stephen McConnel")]
		[TestCase(10, "this is something extra on a new page before 8", "levels.png", "Copyright © 2021, USAID \"Okuu keremet!\"")]
		[TestCase(12, "this is something extra after all the original pages", "man.png", "")]
		public void PageAddedWithTextAndPicture(int n, string text, string src, string copyright)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-imageContainer')]/img[@src='{src}']", 1);
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-translationGroup')]/div[contains(@class, 'bloom-editable') and @lang='en' and text()='{text}']", 1);
			// This is the ID for the standard "Basic text and picture" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("adcd48df-e9ab-4a07-afd4-6a24d0398382"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("adcd48df-e9ab-4a07-afd4-6a24d0398382"));
			var imgElt = _contentPages[n].SelectSingleNode(".//img");
			Assert.That(imgElt.Attributes["data-copyright"]?.Value, Is.EqualTo(copyright));
			// Could check other metadata here, but we're basically checking that one call was made which sets it all up.
		}

		[TestCase(9, "This will go on a new page before 8")]
		[TestCase(14, "This will go on a new just-text page at the end")]
		public void PageAddedWithText(int n, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-translationGroup')]/div[contains(@class, 'bloom-editable') and @lang='en' and text()='{text}']", 1);
			// This is the ID for the standard "Just text" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("a31c38d8-c1cb-4eb9-951b-d2840f6a8bdb"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("a31c38d8-c1cb-4eb9-951b-d2840f6a8bdb"));
		}

		[Test]
		public void CoverPagesSurvived()
		{
			AssertThatXmlIn.Element(_firstPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'outsideFrontCover')]", 1);
			AssertThatXmlIn.Element(_lastPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'outsideBackCover')]", 1);
			AssertThatXmlIn.Element(_secondLastPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'insideBackCover')]", 1);
		}

		[TestCase("shirt.png")]
		[TestCase("Mars 3.png")]
		[TestCase("man.png")]
		[TestCase("LakePendOreille.jpg")]
		[TestCase("levels.png")]
		[TestCase("lady24b.png")]
		public void ImageCopiedToOutput(string fileName)
		{
			Assert.That(RobustFile.Exists(Path.Combine(_bookFolder.FolderPath, fileName)));
		}

		[Test]
		public void ImageNotFoundWarningPresent()
		{
			var source = Path.Combine(_spreadsheetFolder, "images/missingBird.png");
			Assert.That(_warnings, Does.Contain("Image \"" + source + "\" on row 4 was not found."));
		}

		[Test]
		public void NoUnexpectedWarnings()
		{
			// Currently we just expect the one noted in ImageNotFoundWarningPresent().
			// Adjust as necessary if we add others.
			Assert.That(_warnings.Count, Is.EqualTo(1));
		}

		[TestCase("by copying the last page")]
		[TestCase("Adding page 7 using a Basic Text")]
		[TestCase("Updating page 3")]
		[TestCase("was not found")]
		[TestCase("Done")]
		public void GotProgressMessage(string message)
		{
			Assert.That(_progressSpy.Messages, Has.Some.Property("Item1").Contains(message));
		}
	}

	/// <summary>
	/// There are some special cases we can only test with a DOM in which the last content page
	/// has neither text nor image elements.
	/// </summary>
	public class SpreadsheetImageAndTextImportToBookWithEmptyLastPageTests
	{
		private HtmlDom _dom;

		private TemporaryFolder _bookFolder;

		public static string PageWithNothing(int pageNumber)
		{
			return string.Format(
				@"	<div class=""bloom-page numberedPage customPage bloom-combinedPage A5Portrait side-right bloom-monolingual"" data-page="""" id=""dc90dbe0-7584-4d9f-bc06-0e0326060054"" data-pagelineage=""adcd48df-e9ab-4a07-afd4-6a24d0398382"" data-page-number=""{0}"" lang="""">
        <div class=""pageLabel"" data-i18n=""TemplateBooks.PageLabel.Basic Text &amp; Picture"" lang=""en"">
            Basic Text &amp; Picture
        </div>

        <div class=""pageDescription"" lang=""en""></div>

        <div class=""split-pane-component marginBox"" style="""">
            
        </div>
    </div>", pageNumber);
		}


		private List<XmlElement> _contentPages;
		private XmlElement _firstPage;
		private XmlElement _lastPage;
		private XmlElement _secondLastPage;
		private List<string> _warnings;
		private string _spreadsheetFolder;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			// We will re-use the images from another test.
			// Conveniently the images are in a folder called "images" which is what the importer expects.
			// So we give it the parent directory of that images folder.
			_spreadsheetFolder = SIL.IO.FileLocationUtilities.GetDirectoryDistributedWithApplication("src/BloomTests/ImageProcessing");

			// Create an HtmlDom for a template to import into. We will give this one a landscape orientation.
			var xml = string.Format(SpreadsheetImageAndTextImportTests.templateDom,
				SpreadsheetImageAndTextImportTests.coverPage
				+ PageWithNothing(1)
				+ SpreadsheetImageAndTextImportTests.insideBackCoverPage
				+ SpreadsheetImageAndTextImportTests.backCoverPage)
				.Replace("A5Portrait", "A5Landscape");
			_dom = new HtmlDom(xml, true);

			// Create an internal spreadsheet with the rows we want to import
			var ss = new InternalSpreadsheet();
			var columnForEn = ss.AddColumnForLang("en", "English");
			var columnForImage = ss.GetColumnForTag(InternalSpreadsheet.ImageSourceColumnLabel);

			// Will insert a just-text page
			var contentRow1 = new ContentRow(ss);
			contentRow1.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow1.SetCell(columnForEn, "this is page 1");

			// Will insert a picture-on-left page
			var contentRow2 = new ContentRow(ss);
			contentRow2.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow2.SetCell(columnForEn, "this is page 2");
			contentRow2.SetCell(columnForImage, "images/lady24b.png");

			// Will insert a just-picture page
			var contentRow3 = new ContentRow(ss);
			contentRow3.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow3.SetCell(columnForImage, "images/man.png");

			_bookFolder = new TemporaryFolder("SpreadsheetImageAndTextImportToBookWithEmptyLastPageTests");

			// Do the import
			var importer = new SpreadsheetImporter(null, _dom, _spreadsheetFolder, _bookFolder.FolderPath);
			_warnings = importer.Import(ss);

			_contentPages = _dom.SafeSelectNodes("//div[contains(@class, 'bloom-page')]").Cast<XmlElement>().ToList();

			// Remove the xmatter to get just the content pages, but save so we can test that too.
			_firstPage = _contentPages[0];
			_contentPages.RemoveAt(0);
			_lastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);
			_secondLastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);

			// (individual test methods will evaluate the result)
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_bookFolder?.Dispose();
		}

		[TestCase(2, "man.png")]
		public void PageAddedWithPicture(int n, string src)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-imageContainer')]/img[@src='{src}']", 1);
			// This is the ID for the standard "Just a picture" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("adcd48df-e9ab-4a07-afd4-6a24d0398385"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("adcd48df-e9ab-4a07-afd4-6a24d0398385"));
		}

		[TestCase(1, "this is page 2", "lady24b.png")]
		public void PageAddedWithTextAndPicture(int n, string text, string src)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-imageContainer')]/img[@src='{src}']", 1);
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-translationGroup')]/div[contains(@class, 'bloom-editable') and @lang='en' and text()='{text}']", 1);
			// This is the ID for the standard "Basic text and picture" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("7b192144-527c-417c-a2cb-1fb5e78bf38a"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("7b192144-527c-417c-a2cb-1fb5e78bf38a"));
		}

		[TestCase(0, "this is page 1")]
		public void PageAddedWithText(int n, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[contains(@class, 'bloom-translationGroup')]/div[contains(@class, 'bloom-editable') and @lang='en' and text()='{text}']", 1);
			// This is the ID for the standard "Just text" page
			Assert.That(_contentPages[n].Attributes["data-pagelineage"].Value, Does.Contain("a31c38d8-c1cb-4eb9-951b-d2840f6a8bdb"));
			Assert.That(_contentPages[n].Attributes["id"].Value, Is.Not.EqualTo("a31c38d8-c1cb-4eb9-951b-d2840f6a8bdb"));
		}

		[Test]
		public void CoverPagesSurvived()
		{
			AssertThatXmlIn.Element(_firstPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'outsideFrontCover')]", 1);
			AssertThatXmlIn.Element(_lastPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'outsideBackCover')]", 1);
			AssertThatXmlIn.Element(_secondLastPage).HasSpecifiedNumberOfMatchesForXpath("self::div[contains(@class, 'insideBackCover')]", 1);
		}
	}

	/// <summary>
	/// There are some special cases we can only test when the last page of a book has room
	/// for more than one image and text.
	/// </summary>
	public class SpreadsheetImageAndTextImportToBookWithComplexLastPageTests
	{
		private HtmlDom _dom;

		private TemporaryFolder _bookFolder;


		private List<XmlElement> _contentPages;
		private XmlElement _firstPage;
		private XmlElement _lastPage;
		private XmlElement _secondLastPage;
		private List<string> _warnings;
		private string _spreadsheetFolder;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			// We will re-use the images from another test.
			// Conveniently the images are in a folder called "images" which is what the importer expects.
			// So we give it the parent directory of that images folder.
			_spreadsheetFolder = SIL.IO.FileLocationUtilities.GetDirectoryDistributedWithApplication("src/BloomTests/ImageProcessing");

			// Create an HtmlDom for a template to import into
			var xml = string.Format(SpreadsheetImageAndTextImportTests.templateDom,
				SpreadsheetImageAndTextImportTests.coverPage
				+ SpreadsheetImageAndTextImportTests.PageWith2ImagesAnd2Texts(1, 1, 1)
				+ SpreadsheetImageAndTextImportTests.insideBackCoverPage
				+ SpreadsheetImageAndTextImportTests.backCoverPage);
			_dom = new HtmlDom(xml, true);

			// Create an internal spreadsheet with the row content we want to test
			var ss = new InternalSpreadsheet();
			var columnForEn = ss.AddColumnForLang("en", "English");
			var columnForImage = ss.GetColumnForTag(InternalSpreadsheet.ImageSourceColumnLabel);

			// Will fill the first part of the existing last page
			var contentRow1 = new ContentRow(ss);
			contentRow1.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow1.SetCell(columnForImage, "images/lady24b.png");
			contentRow1.SetCell(columnForEn, "this is page 1");

			// Will fill the other two slots on the existing last page
			var contentRow2 = new ContentRow(ss);
			contentRow2.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow2.SetCell(columnForEn, "this is the second block on page 1");
			contentRow2.SetCell(columnForImage, "images/shirt.png");

			// Will insert a copy of the last page, with two elements filled in and the
			// others set blank.
			var contentRow3 = new ContentRow(ss);
			contentRow3.AddCell(InternalSpreadsheet.PageContentRowLabel);
			contentRow3.SetCell(columnForEn, "this is the first block on page 2");
			contentRow3.SetCell(columnForImage, "images/man.png");

			_bookFolder = new TemporaryFolder("SpreadsheetImageAndTextImportToBookWithComplexLastPageTests");

			// Do the import
			var importer = new SpreadsheetImporter(null, _dom, _spreadsheetFolder, _bookFolder.FolderPath);
			_warnings = importer.Import(ss);

			_contentPages = _dom.SafeSelectNodes("//div[contains(@class, 'bloom-page')]").Cast<XmlElement>().ToList();

			// Remove the xmatter to get just the content pages, but save so we can test that too.
			_firstPage = _contentPages[0];
			_contentPages.RemoveAt(0);
			_lastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);
			_secondLastPage = _contentPages.Last();
			_contentPages.RemoveAt(_contentPages.Count - 1);

			// (individual test methods will evaluate the result)
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_bookFolder?.Dispose();
		}

		[TestCase(0, "tg1", "this is page 1")]
		[TestCase(0, "tg2", "this is the second block on page 1")]
		[TestCase(1, "tg1", "this is the first block on page 2")]
		public void GotTextOnPageN(int n, string tag, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']/div[@lang='en' and text() = '{text}']", 1);
		}

		[TestCase(1, "tg2")]
		public void NoEnglishTextInBlock(int n, string tag)
		{
			// Sanity check: it does have the TG,
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']", 1);
			// but nothing was added to it.
			AssertThatXmlIn.Element(_contentPages[n]).HasNoMatchForXpath($".//div[@data-test-id='{tag}']/div[@lang='en']");
		}

		[TestCase(0, "ic1", "lady24b.png")]
		[TestCase(0, "ic2", "shirt.png")]
		[TestCase(1, "ic1", "man.png")]
		[TestCase(1, "ic2", "placeHolder.png")]
		public void GotImageSourceOnPageN(int n, string tag, string text)
		{
			AssertThatXmlIn.Element(_contentPages[n]).HasSpecifiedNumberOfMatchesForXpath($".//div[@data-test-id='{tag}']/img[@src='{text}']", 1);
		}

		[Test]
		public void NoUnexpectedWarnings()
		{
			// Currently we don't expect any.
			// Adjust as necessary if we add some.
			Assert.That(_warnings.Count, Is.EqualTo(0));
		}
	}
	public class SpreadsheetImporterErrorTests
	{
		[Test]
		public void PlainTextFileReportsError()
		{
			using (var tempFile = new TempFile("This is complete junk and not a spreadsheet at all"))
			{
				var spy = new ProgressSpy();
				InternalSpreadsheet.ReadFromFile(tempFile.Path, spy);
				Assert.That(spy.Messages, Has.Some.Property("Item1").EqualTo("The input does not appear to be a valid Excel spreadsheet. Import failed."));
			}
		}
		
		[Test]
		public void SpreadsheetWithNoHeaderReportsError()
		{
			// We'll do this one test with a real file that contains nothing like what we expect, just in case
			// it catches anything that fails unexpectedly in the actual file-reading code.
			// The file has a number of cells on one of its rows, which causes this error to be detected in SpreadsheetIO.
			var path = SIL.IO.FileLocationUtilities.GetFileDistributedWithApplication(
				"src/BloomTests/Spreadsheet/spreadsheets/SheetWithNoHeader.xlsx");
			var spy = new ProgressSpy();
			var ss = InternalSpreadsheet.ReadFromFile(path, spy);
			Assert.That(ss, Is.Null);

			// We can also detect this problem on a different path if the spreadsheet doesn't have enough
			// columns to trigger the detection in SpreadsheetIO.
			ss = new InternalSpreadsheet();
			ss.Header.GetRow(0).SetCell(0, "[metadata key]"); // used in earlier versions

			var xml = string.Format(SpreadsheetImageAndTextImportTests.templateDom,
				SpreadsheetImageAndTextImportTests.coverPage
				+ SpreadsheetImageAndTextImportTests.insideBackCoverPage
				+ SpreadsheetImageAndTextImportTests.backCoverPage);
			var dom = new HtmlDom(xml, true);
			var importer = new SpreadsheetImporter(null, dom, null, null);
			Assert.That(importer.Validate(ss, spy), Is.False);
			Assert.That(spy.Messages,
				Has.Some.Property("Item1")
					.EqualTo(
						SpreadsheetImporter.MissingHeaderMessage));
		}

		[Test]
		public void SpreadsheetWithNoContentReportsWarning()
		{
			var spy = new ProgressSpy();
			var ss = new InternalSpreadsheet();
			var xml = string.Format(SpreadsheetImageAndTextImportTests.templateDom,
				SpreadsheetImageAndTextImportTests.coverPage
				+ SpreadsheetImageAndTextImportTests.insideBackCoverPage
				+ SpreadsheetImageAndTextImportTests.backCoverPage);
			var dom = new HtmlDom(xml, true);
			var importer = new SpreadsheetImporter(null, dom, null, null);
			Assert.That(importer.Validate(ss, spy), Is.False);
			Assert.That(spy.Messages,
				Has.Some.Property("Item1")
					.EqualTo(
						"This spreadsheet has no data that Bloom knows how to import. Did you follow the standard format for Bloom spreadsheets?"));

		}
	}
}
