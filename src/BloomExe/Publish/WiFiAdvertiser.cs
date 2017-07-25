﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bloom.Api;
using Bloom.web;
using SIL.Progress;
using ThirdParty.Json.LitJson;

namespace Bloom.Publish
{
	/// <summary>
	/// This class broadcasts a message over the network offering to supply a book to any Android that wants it.
	/// </summary>
	public class WiFiAdvertiser : IDisposable
	{
		// The information we will advertise.
		public string BookTitle;
		public string BookVersion;
		public string TitleLanguage;

		private UdpClient _client;
		private Thread _thread;
		private IPEndPoint _endPoint;
		// The port on which we advertise.
		// ChorusHub uses 5911 to advertise. Bloom looks for a port for its server at 8089 and 10 following ports.
		// https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers shows a lot of ports in use around 8089,
		// but nothing between 5900 and 5931. Decided to use a number similar to ChorusHub.
		private const int Port = 5913; // must match port in BloomReader NewBookListenerService.startListenForUDPBroadcast
		private string _currentIpAddress;
		private byte[] _sendBytes; // Data we send in each advertisement packet

		internal WiFiAdvertiser(WebSocketProgress progress)
		{
			Progress = progress;
		}

		private WebSocketProgress Progress { get; }

		public void Start()
		{
			// The doc seems to indicate that EnableBroadcast is required for doing broadcasts.
			// In practice it seems to be required on Mono but not on Windows.
			// This may be fixed in a later version of one platform or the other, but please
			// test both if tempted to remove it.
			_client = new UdpClient
			{
				EnableBroadcast = true
			};
			_endPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), Port);
			_thread = new Thread(Work);
			_thread.Start();
		}

		private void Work()
		{
			try
			{
				while (true)
				{
					UpdateAdvertisementBasedOnCurrentIpAddress();
					_client.BeginSend(_sendBytes, _sendBytes.Length, _endPoint, SendCallback, _client);
					Thread.Sleep(1000);
				}
			}
			catch (ThreadAbortException)
			{
				//Progress.WriteVerbose("Advertiser Thread Aborting (that's normal)");
				_client.Close();
			}
			catch (Exception error)
			{
				Progress.WriteError("Application", string.Format("Error in Advertiser: {0}", error.Message), EventLogEntryType.Error);
			}
		}
		public static void SendCallback(IAsyncResult args)
		{
		}

		/// <summary>
		/// Since this is typically not a real "server", its ipaddress could be assigned dynamically,
		/// and could change each time someone wakes it up.
		/// </summary>
		private void UpdateAdvertisementBasedOnCurrentIpAddress()
		{
			if (_currentIpAddress != GetLocalIpAddress())
			{
				_currentIpAddress = GetLocalIpAddress();
				dynamic advertisement = new DynamicJson();
				advertisement.Title = BookTitle;
				advertisement.Version = BookVersion;
				advertisement.Language = TitleLanguage;
				advertisement.ProtocolVersion = BloomReaderPublisher.ProtocolVersion;

				_sendBytes = Encoding.UTF8.GetBytes(advertisement.ToString());
				//EventLog.WriteEntry("Application", "Serving at http://" + _currentIpAddress + ":" + ChorusHubOptions.MercurialPort, EventLogEntryType.Information);
			}
		}

		/// <summary>
		/// The intent here is to get an IP address by which this computer can be found on the local subnet.
		/// This is ambiguous if the computer has more than one IP address (typically for an Ethernet and WiFi adapter).
		/// Early experiments indicate that things work whichever one is used, assuming the networks are connected.
		/// Eventually we may want to prefer WiFi if available (see code in HearThis), or even broadcast on all of them.
		/// </summary>
		/// <returns></returns>
		private string GetLocalIpAddress()
		{
			string localIp = null;
			var host = Dns.GetHostEntry(Dns.GetHostName());

			foreach (var ipAddress in host.AddressList.Where(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork))
			{
				if (localIp != null)
				{
					if (host.AddressList.Length > 1)
					{
						//EventLog.WriteEntry("Application", "Warning: this machine has more than one IP address", EventLogEntryType.Warning);
					}
				}
				localIp = ipAddress.ToString();
			}
			return localIp ?? "Could not determine IP Address!";
		}

		public void Stop()
		{
			if (_thread == null)
				return;

			//EventLog.WriteEntry("Application", "Advertiser Stopping...", EventLogEntryType.Information);
			_thread.Abort();
			_thread.Join(2 * 1000);
			_thread = null;
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
