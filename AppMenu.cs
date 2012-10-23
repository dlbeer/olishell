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
    class AppMenu
    {
	MenuBar			menuBar;
	DebugManager		debugManager;
	PreferencesDialog	preferences;

	public AppMenu(DebugManager mgr, AccelGroup agr,
		       Settings set, Window parent)
	{
	    debugManager = mgr;
	    menuBar = new MenuBar();
	    preferences = new PreferencesDialog(set, parent);

	    menuBar.Append(CreateFileMenu(agr));
	    menuBar.Append(CreateEditMenu(agr));
	    menuBar.Append(CreateDebuggerMenu(agr));
	    menuBar.Append(CreateHelpMenu(agr));
	}

	public Widget View
	{
	    get { return menuBar; }
	}

	// Create "File" menu
	MenuItem CreateFileMenu(AccelGroup agr)
	{
	    MenuItem file = new MenuItem("_File");
	    Menu fileMenu = new Menu();
	    file.Submenu = fileMenu;

	    MenuItem quit = new ImageMenuItem(Stock.Quit, agr);
	    quit.Activated += (obj, evt) => Application.Quit();
	    fileMenu.Append(quit);

	    return file;
	}

	// Create "Edit" menu
	MenuItem CreateEditMenu(AccelGroup agr)
	{
	    MenuItem edit = new MenuItem("_Edit");
	    Menu editMenu = new Menu();
	    edit.Submenu = editMenu;

	    MenuItem prefs = new ImageMenuItem(Stock.Preferences, agr);
	    prefs.Activated += (obj, evt) => preferences.Run();
	    editMenu.Append(prefs);

	    return edit;
	}

	// Create "Debugger" menu
	MenuItem CreateDebuggerMenu(AccelGroup agr)
	{
	    MenuItem dbg = new MenuItem("_Debugger");
	    Menu dbgMenu = new Menu();
	    dbg.Submenu = dbgMenu;

	    MenuItem intr = new ImageMenuItem(Stock.Cancel, agr);
	    ((Label)intr.Children[0]).Text = "Interrupt";
	    intr.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F9, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    intr.Activated += OnDebuggerStop;
	    dbgMenu.Append(intr);

	    return dbg;
	}

	// Debugger -> Stop
	void OnDebuggerStop(object sender, EventArgs args)
	{
	    debugManager.SendInterrupt();
	}

	// Create "Help" menu
	MenuItem CreateHelpMenu(AccelGroup agr)
	{
	    MenuItem help = new MenuItem("_Help");
	    Menu helpMenu = new Menu();
	    help.Submenu = helpMenu;

	    MenuItem about = new ImageMenuItem(Stock.About, agr);
	    about.Activated += OnHelpAbout;
	    helpMenu.Append(about);

	    return help;
	}

	// Help -> About
	void OnHelpAbout(object sender, EventArgs args)
	{
	    var dlg = new AboutDialog();

	    dlg.Title = "Olishell";
	    dlg.ProgramName = "Olishell";
	    dlg.WrapLicense = true;
	    dlg.Copyright = "Copyright (C) 2012 Olimex Ltd";
	    dlg.License =
		"This program is free software; you can redistribute it " +
		"and/or modify it under the terms of the GNU General " +
		"Public License as published by the Free Software " +
		"Foundation; either version 2 of the License, or (at " +
		"your option) any later version.";

	    dlg.Run();
	    dlg.Hide();
	}
    }
}
