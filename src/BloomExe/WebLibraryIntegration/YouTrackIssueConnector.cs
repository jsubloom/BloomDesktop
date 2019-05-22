﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bloom.web;
using YouTrackSharp.Infrastructure;
using YouTrackSharp.Issues;


namespace Bloom.WebLibraryIntegration	// Review: Could posisibly put in Bloom.web or Bloom.Communication instead?
{
	// TODO: Are all these member variables needed? How about making a static method instead?
	// Well, I could see YouTrackConnection and maybe issueManagement as being pretty legit non-method variables
	public class YouTrackIssueConnector
	{
		private static YouTrackIssueConnector _instance;

		protected string _youTrackProjectKey;

		private readonly Connection _youTrackConnection = new Connection(UrlLookup.LookupUrl(UrlType.IssueTrackingSystemBackend, false, true), 0 /* BL-5500 don't specify port */, true, "youtrack");
		private IssueManagement _issueManagement;
		private dynamic _youTrackIssue;
		private string _youTrackIssueId = "unknown";

		/// <summary>
		/// A new instance should be constructed for every issue.
		/// </summary>
		public YouTrackIssueConnector(string projectKey)
		{
			_youTrackProjectKey = projectKey;
		}

		public void AddAttachment(string file)
		{
			_issueManagement.AttachFileToIssue(_youTrackIssueId, file);
		}

		/// <summary>
		/// Using YouTrackSharp here. We can't submit
		/// the report as if it were from this person, even if they have an account (well, not without
		/// asking them for credentials, which is just not gonna happen). So we submit with an
		/// account we created just for this purpose, "auto_report_creator".
		/// </summary>
		public string SubmitToYouTrack(string summary, string description)
		{
			_youTrackConnection.Authenticate("auto_report_creator", "thisIsInOpenSourceCode");
			_issueManagement = new IssueManagement(_youTrackConnection);
			_youTrackIssue = new Issue();
			_youTrackIssue.ProjectShortName = _youTrackProjectKey;
			_youTrackIssue.Type = "Awaiting Classification";
			_youTrackIssue.Summary = summary;
			_youTrackIssue.Description = description;
			_youTrackIssueId = _issueManagement.CreateIssue(_youTrackIssue);

			return _youTrackIssueId;
		}

		public void UpdateIssue(string descriptionContentsToAppend)
		{
			_youTrackIssue.Description += System.Environment.NewLine + descriptionContentsToAppend;
			_issueManagement.UpdateIssue(_youTrackIssueId, _youTrackIssue.Summary, _youTrackIssue.Description);
		}
	}
}
