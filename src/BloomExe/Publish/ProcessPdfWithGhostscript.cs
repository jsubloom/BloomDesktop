﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Bloom.ToPalaso;
using L10NSharp;
using SIL.CommandLineProcessing;
using SIL.IO;
using SIL.Progress;
using SIL.PlatformUtilities;
using System.Text;
using SIL.Reporting;
using TempFile = SIL.IO.TempFile;

namespace Bloom.Publish
{
	/// <summary>
	/// The Geckofx/Mozilla code used to create PDF files can create truly huge files in the hundreds of megabytes
	/// if not bigger.  ghostscript can reduce the size of these files by 90-95%.
	/// The Geckofx/Mozilla code also produces RGB color by default (which is what cameras produce).  Some printers
	/// accept only CMYK color.  ghostscript can also convert from RGB to CMYK.
	/// This class wraps invoking the ghostscript program to do these tasks.
	/// </summary>
	/// <remarks>
	/// See http://issues.bloomlibrary.org/youtrack/issue/BL-3721 and http://issues.bloomlibrary.org/youtrack/issue/BL-4457.
	/// </remarks>
	public class ProcessPdfWithGhostscript
	{
		public string _inputPdfPath;
		public string _outputPdfPath;

		private readonly BackgroundWorker _worker;

		public enum OutputType {
			DesktopPrinting,	// shrink images to 600dpi in compressed format
			Printshop		// shrink images to 300dpi, convert color to CMYK (-dPDFSETTINGS=/prepress + other options)
		};
		private readonly OutputType _type;

		public ProcessPdfWithGhostscript(OutputType type, BackgroundWorker worker)
		{
			_type = type;
			_worker = worker;
		}

		/// <summary>
		/// Process the input PDF file by compressing images and/or by converting color to CMYK.  The operations
		/// to perform are established by the constructor.
		/// </summary>
		public void ProcessPdfFile(string inputFile, string outputFile)
		{
			_inputPdfPath = inputFile;
			_outputPdfPath = outputFile;
			var exePath = "/usr/bin/gs";
			if (SIL.PlatformUtilities.Platform.IsWindows)
				exePath = FindGhostcriptOnWindows();
			if (!String.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
			{
				if (_worker != null)
					_worker.ReportProgress(0, GetSpecificStatus());
				using (var tempPdfFile = TempFile.WithExtension(".pdf"))
				{
					var runner = new CommandLineRunner();
					var arguments = GetArguments(tempPdfFile.Path);
					var fromDirectory = String.Empty;
					var progress = new NullProgress();	// I can't figure out how to use any IProgress based code, but we show progress okay as is.
					var res = runner.Start(exePath, arguments, Encoding.UTF8, fromDirectory, 3600, progress, ProcessGhostcriptReporting);
					if (res.DidTimeOut || !RobustFile.Exists(tempPdfFile.Path))
					{
						if (_inputPdfPath != _outputPdfPath)
							RobustFile.Copy(_inputPdfPath, _outputPdfPath, true);
						return;
					}
					// If the process made the file larger and didn't change the color scheme, ignore the result.
					var oldInfo = new FileInfo(_inputPdfPath);
					var newInfo = new FileInfo(tempPdfFile.Path);
					if (newInfo.Length < oldInfo.Length || _type == OutputType.Printshop)
						RobustFile.Copy(tempPdfFile.Path, _outputPdfPath, true);
					else if (_inputPdfPath != _outputPdfPath)
						RobustFile.Copy(_inputPdfPath, _outputPdfPath, true);
				}
			}
			else
			{
				// This shouldn't happen.  Linux Bloom package depends on the ghostscript package, and we'll include
				// ghostscript files in our installer to ensure it's available on Windows.  But we have this code here
				// as a failsafe fallback reminding the developers to ensure this installation work happens.
				Debug.WriteLine("ghostscript is not installed, so Bloom cannot process the PDF file.");
				if (_inputPdfPath != _outputPdfPath)
					RobustFile.Copy(_inputPdfPath, _outputPdfPath, true);
			}
		}

		/// <summary>
		/// Try to find ghostscript on the Windows system, looking in the likeliest installation
		/// locations and trying to not depend on the exact version installed.
		/// </summary>
		/// <returns>path to the ghostscript program, or null if not found</returns>
		/// <remarks>
		/// Ghostscript 9.21 for 64-bit Windows 10 installs by default as
		/// C:\Program Files\gs\gs9.21\bin\gswin64c.exe or C:\Program Files\gs\gs9.21\bin\gswin64.exe.
		/// The former uses the current console window, the latter pops up its own command window.
		/// </remarks>
		private string FindGhostcriptOnWindows()
		{
			// TODO: if we decide to distribute GS with Bloom, enhance this to look first wherever we
			// install GS in the user area.
			var baseName = "gswin32";
			if (Environment.Is64BitOperatingSystem)
				baseName = "gswin64";
			// Look for the four most common installation locations.
			var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "gs");
			// GetFolderPath returns the wrong value on x64 system for x86 programs.
			// See https://stackoverflow.com/questions/23304823/environment-specialfolder-programfiles-returns-the-wrong-directory.
			baseDir = baseDir.Replace(" (x86)", "");
			if (!Directory.Exists(baseDir))
			{
				baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "gs");
				baseName = "gswin32";
			}
			if (!Directory.Exists(baseDir))
				return null;
			foreach (var versionDir in Directory.GetDirectories(baseDir))
			{
				var prog = Path.Combine(versionDir, "bin", baseName + "c.exe");
				if (File.Exists(prog))
					return prog;
				prog = Path.Combine(versionDir, "bin", baseName + ".exe");
				if (File.Exists(prog))
					return prog;
			}
			return null;
		}

		private string GetArguments(string tempFile)
		{
			var bldr = new StringBuilder();
			// CompatibilityLevel=1.4 - Acrobat 5.0
			// CompatibilityLevel=1.5 - Acrobat 6.0
			// CompatibilityLevel=1.6 - Acrobat 7.0
			// CompatibilityLevel=1.7 - Acrobat 8.0/9.0 (~2008)
			bldr.Append("-sDEVICE=pdfwrite -dBATCH -dNOPAUSE -dCompatibilityLevel=1.7");	// REVIEW: is this the right compatibility level?
			switch (_type)
			{
			case OutputType.DesktopPrinting:
				// See http://issues.bloomlibrary.org/youtrack/issue/BL-3721 for the need for this operation.
				// See also https://stackoverflow.com/questions/9497120/how-to-downsample-images-within-pdf-file and
				// https://superuser.com/questions/435410/where-are-ghostscript-options-switches-documented.  We are trying
				// for "press quality" (or better), but leaving the color as RGB.
				bldr.Append(" -dColorImageResolution=600");
				bldr.Append(" -dGrayImageResolution=600");
				bldr.Append(" -dMonoImageResolution=1200");
				break;
			case OutputType.Printshop:
				// This reduces images to 300dpi, converting the color to CMYK.
				bldr.Append(" -dPDFSETTINGS=/prepress");
				bldr.Append(" -sColorConversionStrategy=CMYK");
				bldr.Append(" -dOverrideICC=true");
				var rgbProfile = FileLocator.GetFileDistributedWithApplication("ColorProfiles/RGB/AdobeRGB1998.icc");
				var cmykProfile = FileLocator.GetFileDistributedWithApplication("ColorProfiles/CMYK/USWebCoatedSWOP.icc");
				bldr.AppendFormat(" -sDefaultRGBProfile=\"{0}\"", rgbProfile);
				bldr.AppendFormat(" -sDefaultCMYKProfile=\"{0}\"", cmykProfile);
				bldr.AppendFormat(" -sOutputICCProfile=\"{0}\"", cmykProfile);
				break;
			}
			bldr.Append(" -dDownsampleColorImages=true -dColorImageDownsampleThreshold=1.0");
			bldr.Append(" -dDownsampleGrayImages=true -dGrayImageDownsampleThreshold=1.0");
			bldr.Append(" -dDownsampleMonoImages=true -dMonoImageDownsampleThreshold=1.0");
			bldr.AppendFormat($" -sOutputFile=\"{tempFile}\" \"{_inputPdfPath}\"");
			return bldr.ToString();
		}

		private string GetSpecificStatus()
		{
			switch (_type)
			{
			case OutputType.DesktopPrinting:
				return L10NSharp.LocalizationManager.GetString(@"PublishTab.PdfMaker.Compress",
					"Compressing PDF",
					@"Message displayed in a progress report dialog box");
			case OutputType.Printshop:
				return L10NSharp.LocalizationManager.GetString(@"PublishTab.PdfMaker.CompressConvertColor",
					"Compressing PDF & Converting Color to CMYK",
					@"Message displayed in a progress report dialog box");
								break;
			}
			return String.Empty;
		}

		int _firstPage;
		int _numPages;

		// Magic strings to match for progress reporting.  ghostscript itself doesn't seem to be localized
		// (maybe because it's a command line program?)
		const string kProcessingPages = "Processing pages ";
		const string kThroughWithSpaces = " through ";
		const string kPage = "Page ";

		/// <summary>
		/// Use the stdout progress reporting to move the GUI progress report along.  This does assume that
		/// ghostscript doesn't have any localization since it's parsing English strings to extract information.
		/// </summary>
		private void ProcessGhostcriptReporting(string line)
		{
			if (_worker == null)
				return;		// nothing here will have any effect anyway
			//Debug.WriteLine(String.Format("DEBUG gs report line = \"{0}\"", line));
			if (line.StartsWith(kProcessingPages) && line.Contains(kThroughWithSpaces))
			{
				_firstPage = 0;
				int lastPage = 0;
				_numPages = 0;
				// Get the first and last page numbers processed and the total number of pages.
				var idxNumber = kProcessingPages.Length;
				var idxMid = line.IndexOf(kThroughWithSpaces);
				if (idxMid > idxNumber && !Int32.TryParse(line.Substring(idxNumber, idxMid - idxNumber), out _firstPage))
					_firstPage = 0;
				idxNumber = idxMid + kThroughWithSpaces.Length;
				var idxPeriod = line.IndexOf(".", idxNumber);
				if (idxPeriod > idxNumber && !Int32.TryParse(line.Substring(idxNumber, idxPeriod - idxNumber), out lastPage))
					lastPage = 0;
				if (_firstPage > 0 && lastPage > 0)
					_numPages = lastPage - _firstPage + 1;
			}
			else if (line.StartsWith(kPage) && _numPages > 0)
			{
				// Get the current page number and adjust the progress dialog appropriately.
				int pageNumber = 0;
				if (Int32.TryParse(line.Substring(kPage.Length), out pageNumber) && _numPages > 0)
				{
					try
					{
						var percentage = (int)(100.0F * (float)(pageNumber - _firstPage) / (float)_numPages);
						_worker.ReportProgress(percentage);
					}
					catch (ObjectDisposedException e)
					{
						// Don't worry about not being able to update while/after the progress dialog is closing/has closed.
					}
				}
			}
		}
	}
}
