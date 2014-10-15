﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using L10NSharp;

namespace Bloom.CollectionTab
{
	public partial class MakeReaderTemplateBloomPackDlg : Form
	{
		private string _willCarrySettingsOriginal;
		public MakeReaderTemplateBloomPackDlg()
		{
			InitializeComponent();
			_willCarrySettingsOriginal = _willCarrySettingsLabel.Text;
		}

		public void SetLanguage(string name)
		{
			_willCarrySettingsLabel.Text = string.Format(_willCarrySettingsOriginal, name);
		}

		public void SetTitles(IEnumerable<string> files)
		{
			_bookList.SuspendLayout();
			_bookList.Items.Clear();
			_bookList.Items.AddRange(files.ToArray());
			_bookList.ResumeLayout();
		}
	}
}
