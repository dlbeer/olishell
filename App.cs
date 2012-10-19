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
    class App
    {
	public readonly Window MainWin = new Window("Olishell");
	DebugView debugView = new DebugView();
	PowerView powerView = new PowerView();

	public App()
	{
	    AccelGroup agr = new AccelGroup();
	    VBox vb = new VBox(false, 3);
	    VPaned hp = new VPaned();

	    MainWin.Resize(700, 500);
	    MainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    MainWin.AddAccelGroup(agr);

	    hp.Add(powerView.View);
	    hp.Add(debugView.View);

	    vb.PackStart(CreateMenuBar(agr), false, false, 0);
	    vb.PackEnd(hp, true, true, 0);
	    MainWin.Add(vb);

	    // FIXME: testing
	    debugView.Debugger = new Debugger("mspdebug", "--embed sim");

	    {
		SampleQueue sq = new SampleQueue(55, 2048);
		int i;
		int[] chunk = new int[512];

		for (i = 1; i < chunk.Length / 2; i++)
		{
		    chunk[i] = 3000 + (i * 327 + i * 45) % 300;
		    chunk[i + chunk.Length / 2] =
			5000 + (i * 327 + i * 45) % 300;
		}

		sq.Push(chunk);
		sq.Push(chunk);
		sq.Push(chunk);
		sq.Push(chunk);
		powerView.Model = sq;
	    }
	}

	// Create and return the menu bar
	MenuBar CreateMenuBar(AccelGroup agr)
	{
	    MenuBar mb = new MenuBar();

	    mb.Append(CreateFileMenu(agr));
	    mb.Append(CreateDebuggerMenu(agr));
	    mb.Append(CreateHelpMenu(agr));

	    return mb;
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
	    Debugger dbg = debugView.Debugger;

	    if (dbg != null)
		dbg.Cancel.Raise();
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

	// Do not call this function from the Gtk main loop!
	void DebuggerSyncExit()
	{
	    Debugger dbg = debugView.Debugger;

	    if (dbg != null)
	    {
		// Interrupt the debugger and signal end-of-input
		dbg.Cancel.Raise();
		dbg.Commands.Close();

		// Wait for the output to drain
		while (!dbg.Output.IsClosed)
		{
		    Debugger.Message msg;

		    ITC.Sync.Wait(dbg.Output);
		    while (dbg.Output.TryReceive(out msg));
		}
	    }
	}

	public static void Main()
	{
	    Application.Init();

	    var app = new App();
	    app.MainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    app.MainWin.ShowAll();
	    Application.Run();

	    app.DebuggerSyncExit();
	}
    }
}
