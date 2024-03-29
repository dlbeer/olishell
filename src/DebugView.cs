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
	DebugManager	debugManager;

	public DebugView(Settings set, DebugManager mgr)
	{
	    debugManager = mgr;
	    layout = new Table(2, 2, false);
	    runStop = new Button("Interrupt");
	    command = new Entry();
	    log = new ConsoleLog(set);

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

	    runStop.SetSizeRequest(80, -1);

	    command.Sensitive = false;
	    runStop.Sensitive = false;

	    mgr.MessageReceived += OnDebugOutput;
	    mgr.DebuggerBusy += OnBusy;
	    mgr.DebuggerReady += OnReady;
	    mgr.DebuggerStarted += OnStarted;
	    mgr.DebuggerExited += OnExited;
	    layout.Destroyed += OnDestroy;
	}

	public Widget View
	{
	    get { return layout; }
	}

	void OnDestroy(object sender, EventArgs args)
	{
	    debugManager.MessageReceived -= OnDebugOutput;
	    debugManager.DebuggerBusy -= OnBusy;
	    debugManager.DebuggerReady -= OnReady;
	    debugManager.DebuggerStarted -= OnStarted;
	    debugManager.DebuggerExited -= OnExited;
	}

	void OnDebugOutput(object sender, DebugManager.MessageEventArgs args)
	{
	    log.AddLine(args.Message.Text);
	}

	public void RunCommand(string text)
	{
	    int nl = text.IndexOf('\n');

	    if (nl >= 0)
		text = text.Substring(0, nl);

	    log.AddLine("\x1b[1m==>\x1b[0m " + text);
	    debugManager.SendCommand(text);
	}

	void OnCommand(object sender, EventArgs args)
	{
	    RunCommand(command.Text);
	    command.Text = "";
	}

	void OnRunStop(object sender, EventArgs args)
	{
	    if (debugManager.IsReady)
		OnCommand(sender, args);
	    else
		debugManager.SendInterrupt();
	}

	void OnStarted(object sender, EventArgs args)
	{
	    runStop.Label = "Interrupt";
	    command.Sensitive = false;
	    runStop.Sensitive = true;
	    log.AddLine("\x1b[1mDebugger started\x1b[0m");
	}

	void OnExited(object sender, EventArgs args)
	{
	    runStop.Sensitive = false;
	    command.Sensitive = false;
	    log.AddLine("\x1b[1mDebugger exited\x1b[0m");
	}

	void OnBusy(object sender, EventArgs args)
	{
	    runStop.Label = "Interrupt";
	    command.Sensitive = false;
	}

	void OnReady(object sender, EventArgs args)
	{
	    runStop.Label = "Run";
	    command.Sensitive = true;
	    command.GrabFocus();
	}

	public void SelectAll()
	{
	    log.SelectAll();
	}

	public void CopyText()
	{
	    log.CopyText();
	}

	public void ClearText()
	{
	    log.Clear();
	}

	public string Transcript
	{
	    get { return log.Transcript; }
	}

	public void Rescroll()
	{
	    log.Rescroll();
	}
    }
}
