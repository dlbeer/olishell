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
	Window mainWin = new Window("Olishell");
	AppMenu menu;
	DebugPane debugPane;
	Settings settings;

	public App(Settings set, DebugManager mgr)
	{
	    settings = set;
	    debugPane = new DebugPane(set, mgr);

	    AccelGroup agr = new AccelGroup();
	    menu = new AppMenu(mgr, agr, set, mainWin, debugPane);

	    VBox vb = new VBox(false, 3);

	    mainWin.Resize(settings.WindowWidth, settings.WindowHeight);
	    mainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    mainWin.AddAccelGroup(agr);

	    vb.PackStart(menu.View, false, false, 0);
	    vb.PackEnd(debugPane.View, true, true, 0);
	    mainWin.Add(vb);

	    mainWin.DeleteEvent += OnDeleteEvent;
	    mainWin.ShowAll();
	}

	void OnDeleteEvent(object sender, EventArgs args)
	{
	    debugPane.SaveLayout();
	    mainWin.GetSize(out settings.WindowWidth,
			    out settings.WindowHeight);
	    Application.Quit();
	}

	public static void Main()
	{
	    Application.Init();

	    try
	    {
		Settings settings = Settings.Load();
		DebugManager mgr = new DebugManager(settings);

		new App(settings, mgr);

		Application.Run();

		// Synchronously terminate the debugger
		mgr.Terminate();
		while (mgr.IsRunning)
		    Application.RunIteration();

		settings.Save();
	    }
	    catch (Exception ex)
	    {
		MessageDialog dlg = new MessageDialog
		    (null, DialogFlags.Modal, MessageType.Error,
		     ButtonsType.Ok, "Unhandled exception: {0}",
		     ex.Message);
		dlg.Title = "Olishell";

		dlg.Run();

		throw ex;
	    }
	}
    }
}
