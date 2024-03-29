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
using System.Text;
using System.IO;
using System.Collections.Generic;
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

	// Command menu items
	List<MenuItem>		commandMacros = new List<MenuItem>();

	public AppMenu(DebugManager mgr, AccelGroup agr,
		       Settings set, Window parent,
		       DebugPane pane, string argsOverride)
	{
	    settings = set;
	    debugPane = pane;
	    debugManager = mgr;
	    menuBar = new MenuBar();
	    preferences = new PreferencesDialog(set, parent, argsOverride);

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

	    foreach (MenuItem m in commandMacros)
		m.Sensitive = false;
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

	    foreach (MenuItem m in commandMacros)
		m.Sensitive = false;
	}

	void OnDebuggerReady(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = false;

	    foreach (MenuItem m in commandMacros)
		m.Sensitive = true;
	}

	void OnDebuggerStarted(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = false;
	    debuggerStop.Sensitive = true;
	    debuggerInterrupt.Sensitive = true;

	    foreach (MenuItem m in commandMacros)
		m.Sensitive = false;
	}

	void OnDebuggerExited(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;

	    foreach (MenuItem m in commandMacros)
		m.Sensitive = false;
	}

	// Create "File" menu
	MenuItem CreateFileMenu(AccelGroup agr)
	{
	    MenuItem file = new MenuItem("_File");
	    Menu fileMenu = new Menu();
	    file.Submenu = fileMenu;

	    MenuItem save = new ImageMenuItem(Stock.SaveAs, agr);
	    ((Label)save.Children[0]).Text = "Save transcript...";
	    save.Activated += OnSaveTranscript;
	    fileMenu.Append(save);

	    fileMenu.Append(new SeparatorMenuItem());

	    MenuItem quit = new ImageMenuItem(Stock.Quit, agr);
	    quit.Activated += (obj, evt) => Application.Quit();
	    fileMenu.Append(quit);

	    return file;
	}

	// File -> Save transcript
	void OnSaveTranscript(object sender, EventArgs args)
	{
	    FileChooserDialog dlg = new FileChooserDialog("Save transcript",
		null, FileChooserAction.Save,
		new object[]{Stock.Cancel, ResponseType.Cancel,
			     Stock.Ok, ResponseType.Ok});

	    dlg.DefaultResponse = ResponseType.Ok;

	    for (;;)
	    {
		if ((ResponseType)dlg.Run() == ResponseType.Ok)
		{
		    if (DoSaveTranscript(dlg.Filename))
			break;
		}
		else
		{
		    break;
		}
	    }

	    dlg.Hide();
	}

	bool DoSaveTranscript(string filename)
	{
	    if (File.Exists(filename))
	    {
		MessageDialog dlg = new MessageDialog(null,
		    DialogFlags.Modal, MessageType.Question,
		    ButtonsType.YesNo,
		    "Do you want to overwrite the existing file?");

		ResponseType rt = (ResponseType)dlg.Run();
		dlg.Hide();

		if (rt != ResponseType.Yes)
		    return false;
	    }

	    try
	    {
		using (Stream s = File.Create(filename))
		    using (StreamWriter sw = new StreamWriter(s,
				Encoding.UTF8))
			sw.Write(debugPane.DebugView.Transcript);
	    }
	    catch (Exception ex)
	    {
		MessageDialog dlg = new MessageDialog
		    (null, DialogFlags.Modal, MessageType.Error,
		     ButtonsType.Ok, "Can't write transcript: {0}",
		     ex.Message);

		dlg.Title = "Olishell";
		dlg.Run();
		dlg.Hide();
		return false;
	    }

	    return true;
	}

	// Create "Edit" menu
	MenuItem CreateEditMenu(AccelGroup agr)
	{
	    MenuItem edit = new MenuItem("_Edit");
	    Menu editMenu = new Menu();
	    edit.Submenu = editMenu;

	    MenuItem copy = new ImageMenuItem(Stock.Copy, agr);
	    copy.Activated += (obj, evt) =>
		debugPane.DebugView.CopyText();
	    editMenu.Append(copy);

	    MenuItem selectAll = new ImageMenuItem(Stock.SelectAll, agr);
	    selectAll.Activated += (obj, evt) =>
		debugPane.DebugView.SelectAll();
	    editMenu.Append(selectAll);

	    MenuItem clear = new ImageMenuItem(Stock.Clear, agr);
	    ((Label)clear.Children[0]).Text = "Clear transcript";
	    clear.Activated += (obj, evt) =>
		debugPane.DebugView.ClearText();
	    editMenu.Append(clear);

	    MenuItem clearPower = new ImageMenuItem(Stock.Clear, agr);
	    ((Label)clearPower.Children[0]).Text = "Clear power graph";
	    clearPower.Activated += (obj, evt) => debugManager.ClearPower();
	    editMenu.Append(clearPower);

	    editMenu.Append(new SeparatorMenuItem());

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
	    powerVisible.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F3, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    viewMenu.Append(powerVisible);

	    viewMenu.Append(new SeparatorMenuItem());

	    MenuItem zoomIn = new ImageMenuItem(Stock.ZoomIn, agr);
	    zoomIn.Activated += (obj, evt) =>
		debugPane.PowerView.ZoomIn();
	    zoomIn.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.plus, Gdk.ModifierType.ControlMask,
		AccelFlags.Visible));
	    viewMenu.Append(zoomIn);

	    MenuItem zoomOut = new ImageMenuItem(Stock.ZoomOut, agr);
	    zoomOut.Activated += (obj, evt) =>
		debugPane.PowerView.ZoomOut();
	    zoomOut.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.minus, Gdk.ModifierType.ControlMask,
		AccelFlags.Visible));
	    viewMenu.Append(zoomOut);

	    MenuItem zoomFit = new ImageMenuItem(Stock.ZoomFit, agr);
	    zoomFit.Activated += (obj, evt) =>
		debugPane.PowerView.ZoomFit();
	    zoomFit.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.Key_0, Gdk.ModifierType.ControlMask,
		AccelFlags.Visible));
	    viewMenu.Append(zoomFit);

	    MenuItem zoomFull = new ImageMenuItem(Stock.Zoom100, agr);
	    zoomFull.Activated += (obj, evt) =>
		debugPane.PowerView.ZoomFull();
	    zoomFull.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.Key_1, Gdk.ModifierType.ControlMask,
		AccelFlags.Visible));
	    viewMenu.Append(zoomFull);

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

	    debuggerStart = new MenuItem("_Start debugger");
	    debuggerStart.Activated += OnDebuggerStart;
	    debuggerStart.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F5, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    dbgMenu.Append(debuggerStart);

	    debuggerStop = new MenuItem("_Stop debugger");
	    debuggerStop.Activated += OnDebuggerStop;
	    debuggerStop.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F6, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    dbgMenu.Append(debuggerStop);

	    debuggerInterrupt = new ImageMenuItem(Stock.Cancel, agr);
	    ((Label)debuggerInterrupt.Children[0]).Text = "Interrupt execution";
	    debuggerInterrupt.AddAccelerator("activate", agr,
		new AccelKey(Gdk.Key.F9, Gdk.ModifierType.None,
		AccelFlags.Visible));
	    debuggerInterrupt.Activated += OnDebuggerInterrupt;
	    dbgMenu.Append(debuggerInterrupt);

	    dbgMenu.Append(CreateCommandsMenu(agr));

	    return dbg;
	}

	// Create "Debugger/Commands" menu
	MenuItem CreateCommandsMenu(AccelGroup agr)
	{
	    MenuItem cmd = new MenuItem("_Commands");
	    Menu cmdMenu = new Menu();
	    cmd.Submenu = cmdMenu;

	    MenuItem prog = new ImageMenuItem(Stock.Open, agr);
	    ((Label)prog.Children[0]).Text = "Program...";
	    prog.Activated += OnCommandProgram;
	    cmdMenu.Append(prog);
	    commandMacros.Add(prog);

	    MenuItem reset = new ImageMenuItem(Stock.Clear, agr);
	    ((Label)reset.Children[0]).Text = "Reset";
	    reset.Activated += (obj, evt) =>
		debugPane.DebugView.RunCommand("reset");
	    cmdMenu.Append(reset);
	    commandMacros.Add(reset);

	    MenuItem run = new ImageMenuItem(Stock.GoForward, agr);
	    ((Label)run.Children[0]).Text = "Run";
	    run.Activated += (obj, evt) =>
		debugPane.DebugView.RunCommand("run");
	    cmdMenu.Append(run);
	    commandMacros.Add(run);

	    MenuItem step = new ImageMenuItem(Stock.MediaNext, agr);
	    ((Label)step.Children[0]).Text = "Step";
	    step.Activated += (obj, evt) =>
		debugPane.DebugView.RunCommand("step");
	    cmdMenu.Append(step);
	    commandMacros.Add(step);

	    return cmd;
	}

	// Debugger -> Commands -> Program...
	void OnCommandProgram(object sender, EventArgs args)
	{
	    FileChooserDialog dlg = new FileChooserDialog("Program file",
		null, FileChooserAction.Open,
		new object[]{Stock.Cancel, ResponseType.Cancel,
			     Stock.Ok, ResponseType.Ok});

	    dlg.DefaultResponse = ResponseType.Ok;

	    if ((ResponseType)dlg.Run() == ResponseType.Ok)
		debugPane.DebugView.RunCommand("prog " + dlg.Filename);

	    dlg.Hide();
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
