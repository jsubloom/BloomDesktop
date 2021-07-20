﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bloom.Publish
{
	/// <summary>
	/// Represents the state of the checkbox for whether to publish a language or not.
	/// In addition to the binary Include / Exclude, also allows a "Default" value,
	/// which means that the checkbox has never been explicitly set by the user and should fallback to the default setting for that checkbox.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public enum LangToPublishCheckboxValue
	{
		Default,
		Include,
		Exclude
	}

	public static class LangToPublishCheckboxExtensions
	{
		public static bool? ToBoolOrNull(this LangToPublishCheckboxValue checkboxVal)
		{
			switch (checkboxVal)
			{
				case LangToPublishCheckboxValue.Exclude:
					return false;
				case LangToPublishCheckboxValue.Include:
					return true;
				case LangToPublishCheckboxValue.Default:
					return null;
				default:
					throw new NotImplementedException("Unknown case: " + checkboxVal.ToString());
			}
		}
	}
}
