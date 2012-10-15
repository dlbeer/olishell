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

	public App()
	{
	    MainWin.Resize(700, 500);
	    MainWin.DeleteEvent += (obj, evt) => Application.Quit();

	    debugView = new DebugView();
	    MainWin.Add(debugView.View);

	    // FIXME: testing
	    debugView.Debugger = new Debugger("mspdebug", "--embed sim");
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
