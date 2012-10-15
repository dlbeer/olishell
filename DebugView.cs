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
    class DebugView
    {
	Table		layout;
	Button		runStop;
	Entry		command;
	ConsoleLog	log;

	Debugger	debug;
	ITC.GtkListener	messageEvent;
	ITC.GtkListener	readyEvent;

	public DebugView()
	{
	    layout = new Table(2, 2, false);
	    runStop = new Button("Interrupt");
	    command = new Entry();
	    log = new ConsoleLog();

	    command.Activated += OnCommand;
	    runStop.Clicked += OnRunStop;

	    layout.Attach(log.View, 0, 2, 0, 1,
			  AttachOptions.Fill,
			  AttachOptions.Expand | AttachOptions.Fill,
			  4, 4);
	    layout.Attach(command, 0, 1, 1, 2,
			  AttachOptions.Fill | AttachOptions.Expand,
			  AttachOptions.Fill,
			  4, 4);
	    layout.Attach(runStop, 1, 2, 1, 2,
			  AttachOptions.Fill, AttachOptions.Fill, 4, 4);

	    command.Sensitive = false;
	    runStop.Sensitive = false;
	}

	public Widget View
	{
	    get { return layout; }
	}

	public Debugger Debugger
	{
	    get { return debug; }
	    set
	    {
		if (debug != null)
		    TeardownEvents();

		debug = value;

		if (debug != null)
		    SetupEvents();

		runStop.Label = "Interrupt";
		runStop.Sensitive = (debug != null);
		command.Sensitive = false;
	    }
	}

	void TeardownEvents()
	{
	    messageEvent.Disable();
	    readyEvent.Disable();
	}

	void SetupEvents()
	{
	    messageEvent = new ITC.GtkListener(debug.Output);
	    messageEvent.Signalled += OnDebugOutput;

	    readyEvent = new ITC.GtkListener(debug.Ready);
	    readyEvent.Signalled += OnReady;
	}

	void OnDebugOutput(object sender, EventArgs args)
	{
	    Debugger.Message msg;

	    while (debug.Output.TryReceive(out msg))
		if (msg.Type != Debugger.MessageType.Shell)
		    log.AddLine(msg.Text);

	    if (debug.Output.IsClosed)
		Debugger = null;
	}

	void OnCommand(object sender, EventArgs args)
	{
	    string text = command.Text;

	    command.Text = "";

	    int nl = text.IndexOf('\n');

	    if (nl >= 0)
		text = text.Substring(0, nl);

	    log.AddLine("==> " + text);
	    debug.Commands.Send(text);

	    runStop.Label = "Interrupt";
	    command.Sensitive = false;
	}

	void OnRunStop(object sender, EventArgs args)
	{
	    if (command.Sensitive)
		OnCommand(sender, args);
	    else
		debug.Cancel.Raise();
	}

	void OnReady(object sender, EventArgs args)
	{
	    debug.Ready.Clear();

	    runStop.Label = "Run";
	    command.Sensitive = true;
	}
    }
}
