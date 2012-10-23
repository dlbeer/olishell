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
	Settings		settings;

	MenuBar			menuBar;
	DebugManager		debugManager;
	DebugPane		debugPane;
	PreferencesDialog	preferences;

	// View: power graph visible
	CheckMenuItem		powerVisible;

	// Debugger menu items
	MenuItem		debuggerStart;
	MenuItem		debuggerStop;
	MenuItem		debuggerInterrupt;

	public AppMenu(DebugManager mgr, AccelGroup agr,
		       Settings set, Window parent,
		       DebugPane pane)
	{
	    settings = set;
	    debugPane = pane;
	    debugManager = mgr;
	    menuBar = new MenuBar();
	    preferences = new PreferencesDialog(set, parent);

	    menuBar.Append(CreateFileMenu(agr));
	    menuBar.Append(CreateEditMenu(agr));
	    menuBar.Append(CreateViewMenu(agr));
	    menuBar.Append(CreateDebuggerMenu(agr));
	    menuBar.Append(CreateHelpMenu(agr));

	    debugManager.DebuggerBusy += OnDebuggerBusy;
	    debugManager.DebuggerReady += OnDebuggerReady;
	    debugManager.DebuggerStarted += OnDebuggerStarted;
	    debugManager.DebuggerExited += OnDebuggerExited;

	    menuBar.Destroyed += OnDestroy;

	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;
	}

	public Widget View
	{
	    get { return menuBar; }
	}

	void OnDestroy(object sender, EventArgs args)
	{
	    debugManager.DebuggerBusy -= OnDebuggerBusy;
	    debugManager.DebuggerReady -= OnDebuggerReady;
	    debugManager.DebuggerStarted -= OnDebuggerStarted;
	    debugManager.DebuggerExited -= OnDebuggerExited;
	}

	void OnDebuggerBusy(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = true;
	}

	void OnDebuggerReady(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = false;
	}

	void OnDebuggerStarted(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = false;
	    debuggerStop.Sensitive = true;
	    debuggerInterrupt.Sensitive = true;
	}

	void OnDebuggerExited(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;
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

	    MenuItem copy = new ImageMenuItem(Stock.Copy, agr);
	    copy.Activated += (obj, evt) => debugPane.CopyText();
	    editMenu.Append(copy);

	    MenuItem selectAll = new ImageMenuItem(Stock.SelectAll, agr);
	    selectAll.Activated += (obj, evt) => debugPane.SelectAll();
	    editMenu.Append(selectAll);

	    MenuItem clear = new ImageMenuItem(Stock.Clear, agr);
	    ((Label)clear.Children[0]).Text = "Clear transcript";
	    clear.Activated += (obj, evt) => debugPane.ClearText();
	    editMenu.Append(clear);

	    MenuItem prefs = new ImageMenuItem(Stock.Preferences, agr);
	    prefs.Activated += (obj, evt) => preferences.Run();
	    editMenu.Append(prefs);

	    return edit;
	}

	// Create "View" menu
	MenuItem CreateViewMenu(AccelGroup agr)
	{
	    MenuItem view = new MenuItem("_View");
	    Menu viewMenu = new Menu();
	    view.Submenu = viewMenu;

	    powerVisible = new CheckMenuItem("Show power _graph");
	    powerVisible.Active = settings.PowerGraphVisible;
	    powerVisible.Activated += OnShowPowerGraph;
	    viewMenu.Append(powerVisible);

	    return view;
	}

	// View -> Show power graph
	void OnShowPowerGraph(object sender, EventArgs args)
	{
	    settings.PowerGraphVisible = powerVisible.Active;
	    settings.RaiseRefreshLayout();
	}

	// Create "Debugger" menu
	MenuItem CreateDebuggerMenu(AccelGroup agr)
	{
	    MenuItem dbg = new MenuItem("_Debugger");
	    Menu dbgMenu = new Menu();
	    dbg.Submenu = dbgMenu;

	    debuggerStart = new MenuItem("_Start");
	    debuggerStart.Activated += OnDebuggerStart;
	    dbgMenu.Append(debuggerStart);

	    debuggerStop = new MenuItem("_Stop");
	    debuggerStop.Activated += OnDebuggerStop;
	    dbgMenu.Append(debuggerStop);

	    debuggerInterrupt = new ImageMenuItem(Stock.Cancel, agr);
	    ((Label)debuggerInterrupt.Children[0]).Text = "Interrupt";
	    debuggerInterrupt.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F9, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    debuggerInterrupt.Activated += OnDebuggerInterrupt;
	    dbgMenu.Append(debuggerInterrupt);

	    return dbg;
	}

	// Debugger -> Start
	void OnDebuggerStart(object sender, EventArgs args)
	{
	    debugManager.Start();
	}

	// Debugger -> Stop
	void OnDebuggerStop(object sender, EventArgs args)
	{
	    debugManager.Terminate();
	}

	// Debugger -> Stop
	void OnDebuggerInterrupt(object sender, EventArgs args)
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
