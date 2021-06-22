﻿using Bloom.Book;
using Bloom.Properties;
using Newtonsoft.Json;

namespace Bloom.Api
{
	/// <summary>
	/// Provide the web code access to various app-wide variables
	/// (i.e. wider than collection settings; related to this Bloom Desktop instance).
	/// </summary>
	public class AppApi
	{
		private const string kAppUrlPrefix = "app/";

		private readonly BookSelection _bookSelection;
		private readonly EditBookCommand _editBookCommand;

		public AppApi(BookSelection bookSelection, EditBookCommand editBookCommand)

		{
			_bookSelection = bookSelection;
			_editBookCommand = editBookCommand;
		}

		public void RegisterWithApiHandler(BloomApiHandler apiHandler)
		{
			apiHandler.RegisterEndpointHandler(kAppUrlPrefix + "enabledExperimentalFeatures", request =>
			{
				if (request.HttpMethod == HttpMethods.Get)
				{
					request.ReplyWithText(ExperimentalFeatures.TokensOfEnabledFeatures);
				}
				else // post
				{
					System.Diagnostics.Debug.Fail("We shouldn't ever be using the 'post' version.");
					request.PostSucceeded();
				}
			}, false);
			apiHandler.RegisterEndpointHandler(kAppUrlPrefix + "autoUpdateSoftwareChoice", HandleAutoUpdate, false);


			/* It's not totally clear if these kinds of things fit well in this App api, or if we
			 will want to introduce a separate api for dealing with these kinds of things. I'm
			erring on the side of less classes, code, for now, easy to split later.*/
			apiHandler.RegisterEndpointHandler(kAppUrlPrefix + "editSelectedBook",
				request =>
				{
					_editBookCommand.Raise(_bookSelection.CurrentSelection);
					request.PostSucceeded();
				}, true);

		}

		public void HandleAutoUpdate(ApiRequest request)
		{
			if (request.HttpMethod == HttpMethods.Get)
			{
				var json = JsonConvert.SerializeObject(new
				{
					autoUpdate = Settings.Default.AutoUpdate,
					dialogShown = Settings.Default.AutoUpdateDialogShown
				});
				request.ReplyWithJson(json);
			}
			else // post
			{
				var requestData = DynamicJson.Parse(request.RequiredPostJson());
				Settings.Default.AutoUpdateDialogShown = (int)requestData.dialogShown;
				Settings.Default.AutoUpdate = (bool)requestData.autoUpdate;
				request.PostSucceeded();
			}
		}
	}
}
