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
	DebugView debugView;
	PowerView powerView = new PowerView();
	VPaned pane = new VPaned();
	AppMenu menu;
	Settings settings;

	public App(Settings set, DebugManager mgr)
	{
	    settings = set;
	    AccelGroup agr = new AccelGroup();
	    debugView = new DebugView(mgr);
	    menu = new AppMenu(mgr, agr, set, mainWin);

	    VBox vb = new VBox(false, 3);

	    mainWin.Resize(settings.WindowWidth, settings.WindowHeight);
	    mainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    mainWin.AddAccelGroup(agr);

	    pane.Add(powerView.View);
	    pane.Add(debugView.View);
	    pane.Position = settings.SizerPosition;

	    vb.PackStart(menu.View, false, false, 0);
	    vb.PackEnd(pane, true, true, 0);
	    mainWin.Add(vb);

	    mainWin.DeleteEvent += OnDeleteEvent;
	    mainWin.ShowAll();

	    Test();
	}

	void OnDeleteEvent(object sender, EventArgs args)
	{
	    settings.SizerPosition = pane.Position;
	    mainWin.GetSize(out settings.WindowWidth,
			    out settings.WindowHeight);
	    Application.Quit();
	}

	void Test()
	{
	    // FIXME: testing
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

	public static void Main()
	{
	    Settings settings = Settings.Load();
	    DebugManager mgr = new DebugManager(settings);

	    Application.Init();

	    new App(settings, mgr);

	    Application.Run();

	    // Synchronously terminate the debugger
	    mgr.Terminate();
	    while (mgr.IsRunning)
		Application.RunIteration();

	    settings.Save();
	}
    }
}
