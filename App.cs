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
	public static void Main()
	{
	    DBTest();
	}

	static void DBTest()
	{
	    var db = new Debugger("/usr/local/bin/mspdebug",
		"--embed sim");

	    db.Commands.Send("prog /home/dlbeer/work/fet-fw/20401004.hex");
	    db.Commands.Send("regs");
	    db.Commands.Send("step");
	    db.Commands.Close();

	    for (;;) {
		Debugger.Message msg;

		ITCSync.Wait(db.Output);

		if (!db.Output.TryReceive(out msg))
		    break;

		Console.WriteLine("RECV: " + msg.Text);
	    }
	}

	static void GtkTest()
	{
	    Application.Init();

	    Window win = new Window("Test");
	    win.Resize(640, 480);

	    ConsoleLog log = new ConsoleLog();
	    win.Add(log.View);
	    win.ShowAll();

	    while (Application.EventsPending())
		Application.RunIteration();

	    log.AddLine("Hello world");
	    for (int i = 0; i < 50; i++)
		log.AddLine(Convert.ToString(i));
	    log.AddLine("This is a \x1b[1mtest\x1b[0m of the console");
	    log.AddLine("This should be \x1b[32mgreen");

	    Application.Run();
	}
    }
}
