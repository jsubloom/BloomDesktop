﻿using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

namespace Bloom.ToPalaso
{
	
	public class FontAnalytics
	{
		static HttpClient sClient = new HttpClient();

		public class FontEventType
		{
			public string Value { get; private set; }
			private FontEventType(string value) { Value = value; }
			public static FontEventType ApplyFont  => new FontEventType( "apply-font");
			public static FontEventType PublishPdf  => new FontEventType( "publish-pdf");
			public static FontEventType PublishEbook  => new FontEventType( "publish-ebook");
			public static FontEventType PublishWeb  => new FontEventType( "publish-web");
		}

		public static void Report(string documentId, FontEventType fontEventType, string langTag, bool testOnly, string fontName, string  eventDetails = null) {
			var name = Assembly.GetEntryAssembly().GetName().Name;
			var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			Report(name, version, documentId, fontEventType,  langTag, testOnly, fontName, eventDetails);
		}

		static DateTime _previousCallTime = DateTime.MinValue;
		static List<HttpContent> _queueForSending = new List<HttpContent>();
		static object _lockObject = new object();
		static Timer _timer;
		static bool _currentlyPosting;

		// The current website target of these analytics is throttled to a maximum of 16 events
		// per second.  These are coming in randomly from around the world.  We don't want any
		// one source to saturate the receiver, but we also don't want to delay too much.
		static int _minInterval = 125;	// 8 per second should be safe
		static TimeSpan MinimumSpan = new TimeSpan(0,0,0,0,125);
		public static int MinumumInterval
		{
			get { return _minInterval; }
			set { _minInterval = value; MinimumSpan = new TimeSpan(0,0,0,0,value); }
		}

		public static bool PendingReports
		{
			get
			{
				lock (_lockObject)
				{
					return _currentlyPosting || (_timer != null && _timer.Enabled);
				}
			}
		}

		public static void Report(string applicationName, string applicationVersion, string documentId, FontEventType fontEventType,
									string langTag, bool testOnly, string fontName, string eventDetails = null)
		{
			try
			{
				dynamic data = new JObject();
				data.source = applicationName;
				data.source_version = applicationVersion;
				data.font_name = fontName;
				data.document_id = documentId;
				data.language_tag = langTag;
				data.event_type = fontEventType.Value;
				data.test_only = testOnly;
				if (!string.IsNullOrWhiteSpace(eventDetails))
				{
					data.event_details = eventDetails;
				}
				data.event_time = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
				var dataJson = JsonConvert.SerializeObject(data);
				HttpContent content = new StringContent(dataJson);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

				lock (_lockObject)
				{
					var span = DateTime.UtcNow - _previousCallTime;
					if (_queueForSending.Count > 0 || span < MinimumSpan)
					{
						Debug.WriteLine($"FontAnalytics.Report: queued {fontName}/{langTag}/{applicationName}/{eventDetails}");
						_queueForSending.Add(content);
						if (_timer == null)
						{
							_timer = new Timer(_minInterval);
							_timer.Elapsed += _timer_Elapsed;
						}
						_timer.Enabled = true;
						return;
					}
				}
				_currentlyPosting = true;
				PostAnalytics(content);
			}
			catch (Exception err)
			{
#if DEBUG
				throw err;
#endif
				// normally, swallow
			}
			finally
			{
				_currentlyPosting = false;
			}
		}

		private static void PostAnalytics(HttpContent content)
		{
			try
			{
				var task = sClient.PostAsync("https://font-analytics.languagetechnology.org/api/v1/report-font-use", content);
				_previousCallTime = DateTime.UtcNow;
				task.Wait();    // synchronous equivalent of await
				if (!task.Result.IsSuccessStatusCode)
				{
					Debug.WriteLine(task.Result.ToString());
					var t = task.Result.Content.ReadAsStringAsync();
					t.Wait();   // synchronous equivalent of await
					Debug.WriteLine(t.Result);
				}
			}
			catch (Exception err)
			{
#if DEBUG
				throw err;
#endif
				// normally swallow
			}
		}

		static HttpContent _dequeuedContent = null;

		private static void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (_lockObject)
			{
				var span = DateTime.UtcNow - _previousCallTime;
				if (_queueForSending.Count > 0 && span >= MinimumSpan)
				{
					_timer.Stop();
					_dequeuedContent = _queueForSending[0];
					_queueForSending.RemoveAt(0);
					Debug.WriteLine($"FontAnalytics.Report: dequeued content to post [{_queueForSending.Count} remaining]");
					_currentlyPosting = true;
				}
			}
			if (_dequeuedContent != null)
				PostAnalytics(_dequeuedContent);
			lock (_lockObject)
			{
				_dequeuedContent = null;
				if (_queueForSending.Count > 0)
					_timer.Enabled = true;
				_currentlyPosting = false;
			}
		}
	}
}
