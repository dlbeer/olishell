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
	DebugView debugView;
	PowerView powerView = new PowerView();
	AppMenu menu;

	public App(DebugManager mgr)
	{
	    AccelGroup agr = new AccelGroup();
	    debugView = new DebugView(mgr);
	    menu = new AppMenu(mgr, agr);

	    VBox vb = new VBox(false, 3);
	    VPaned hp = new VPaned();

	    MainWin.Resize(700, 500);
	    MainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    MainWin.AddAccelGroup(agr);

	    hp.Add(powerView.View);
	    hp.Add(debugView.View);

	    vb.PackStart(menu.View, false, false, 0);
	    vb.PackEnd(hp, true, true, 0);
	    MainWin.Add(vb);

	    // FIXME: testing
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

	public static void Main()
	{
	    DebugManager mgr = new DebugManager();

	    Application.Init();

	    var app = new App(mgr);
	    app.MainWin.DeleteEvent += (obj, evt) => Application.Quit();
	    app.MainWin.ShowAll();

	    mgr.Start("/usr/local/bin/mspdebug", "--embed sim");

	    Application.Run();

	    // Synchronously terminate the debugger
	    mgr.Terminate();
	    while (mgr.IsRunning)
		Application.RunIteration();
	}
    }
}
