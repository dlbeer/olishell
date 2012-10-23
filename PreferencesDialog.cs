// Olishell - Olimex MSPDebug shell
// Copyright (C) 2012 Olimex Ltd
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or (at
// your option) any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301
// USA

using System;
using Gtk;

namespace Olishell
{
    class PreferencesDialog
    {
	Settings		settings;
	Dialog			dialog;

	Button			chooseMSPDebug;
	FileChooserDialog	chooseDialog;

	CheckButton		sUseBundledDebugger;
	Entry			sMSPDebugPath;
	Entry			sMSPDebugArgs;

	public PreferencesDialog(Settings set, Window parent)
	{
	    settings = set;

	    dialog = new Dialog("Preferences", parent,
		DialogFlags.Modal | DialogFlags.DestroyWithParent,
		new object[]{Gtk.Stock.Close, -1});

	    var table = new Table(3, 3, false);

	    sUseBundledDebugger = new CheckButton("Use bundled MSPDebug");
	    sUseBundledDebugger.Clicked += OnBundledState;
	    table.Attach(sUseBundledDebugger, 0, 3, 0, 1,
			 AttachOptions.Expand | AttachOptions.Fill,
			 0, 4, 4);

	    Label lbl;

	    lbl = new Label("MSPDebug path:");
	    lbl.SetAlignment(0.0f, 0.5f);
	    table.Attach(lbl, 0, 1, 1, 2, AttachOptions.Fill, 0, 4, 4);
	    sMSPDebugPath = new Entry();
	    table.Attach(sMSPDebugPath, 1, 2, 1, 2,
			 AttachOptions.Expand | AttachOptions.Fill,
			 0, 4, 4);
	    chooseMSPDebug = new Button("Choose...");
	    chooseMSPDebug.Clicked += OnChoose;
	    table.Attach(chooseMSPDebug, 2, 3, 1, 2,
			 AttachOptions.Fill, 0, 4, 4);

	    lbl = new Label("MSPDebug arguments:");
	    lbl.SetAlignment(0.0f, 0.5f);
	    table.Attach(lbl, 0, 1, 2, 3, AttachOptions.Fill, 0, 4, 4);
	    sMSPDebugArgs = new Entry();
	    table.Attach(sMSPDebugArgs, 1, 3, 2, 3,
			 AttachOptions.Expand | AttachOptions.Fill,
			 0, 4, 4);

	    table.ShowAll();
	    ((Container)dialog.Child).Add(table);

	    chooseDialog = new FileChooserDialog("Choose MSPDebug binary",
		dialog, FileChooserAction.Open,
		new object[]{Stock.Cancel, ResponseType.Cancel,
			     Stock.Ok, ResponseType.Ok});
	    chooseDialog.DefaultResponse = ResponseType.Ok;
	}

	void OnBundledState(object sender, EventArgs args)
	{
	    bool enable = !sUseBundledDebugger.Active;

	    sMSPDebugPath.Sensitive = enable;
	    chooseMSPDebug.Sensitive = enable;
	}

	void OnChoose(object sender, EventArgs args)
	{
	    chooseDialog.SetFilename(sMSPDebugPath.Text);
	    ResponseType r = (ResponseType)chooseDialog.Run();
	    chooseDialog.Hide();

	    if (r == ResponseType.Ok)
		sMSPDebugPath.Text = chooseDialog.Filename;
	}

	void Populate()
	{
	    sUseBundledDebugger.Active = settings.UseBundledDebugger;
	    sMSPDebugPath.Text = settings.MSPDebugPath;
	    sMSPDebugArgs.Text = settings.MSPDebugArgs;

	    OnBundledState(this, null);
	}

	void Apply()
	{
	    settings.UseBundledDebugger = sUseBundledDebugger.Active;
	    settings.MSPDebugPath = sMSPDebugPath.Text;
	    settings.MSPDebugArgs = sMSPDebugArgs.Text;
	}

	public void Run()
	{
	    Populate();
	    dialog.Run();
	    dialog.Hide();
	    Apply();
	}
    }
}
