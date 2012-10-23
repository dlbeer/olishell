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
using System.Reflection;
using System.IO;
using Gtk;

namespace Olishell
{
    class DebugManager
    {
	public class MessageEventArgs : EventArgs
	{
	    public readonly Debugger.Message Message;

	    public MessageEventArgs(Debugger.Message m) { Message = m; }
	}

	public delegate void MessageEventHandler(object sender,
		MessageEventArgs args);

	ITC.GtkListener		messageEvent;
	ITC.GtkListener		readyEvent;
	Settings		settings;
	Debugger		debug;
	bool			isReady = false;

	// Public events for state changes.
	public event EventHandler DebuggerStarted;
	public event EventHandler DebuggerReady;
	public event EventHandler DebuggerBusy;
	public event EventHandler DebuggerExited;
	public event MessageEventHandler MessageReceived;

	public DebugManager(Settings set)
	{
	    settings = set;
	}

	// Do we have a running debugger?
	public bool IsRunning
	{
	    get { return debug != null; }
	}

	// Is the debugger ready to receive a command?
	public bool IsReady
	{
	    get { return isReady; }
	}

	// Start a new debugger process if the debugger is not already
	// running.
	public void Start()
	{
	    if (debug != null)
		return;

	    string path = settings.MSPDebugPath;
	    string args = "--embed " + settings.MSPDebugArgs;

	    if (settings.UseBundledDebugger)
		path = Path.Combine
		    (Path.GetDirectoryName
		     (Assembly.GetAssembly(typeof(DebugManager)).CodeBase),
		     "mspdebug.exe");

	    isReady = false;
	    try {
		debug = new Debugger(path, args);
	    }
	    catch (Exception ex)
	    {
		MessageDialog dlg = new MessageDialog
		    (null, DialogFlags.Modal, MessageType.Error,
		     ButtonsType.Ok, "Can't start debugger: {0}",
		     ex.Message);

		dlg.Title = "Olishell";
		dlg.Run();
		dlg.Hide();
		return;
	    }

	    messageEvent = new ITC.GtkListener(debug.Output);
	    messageEvent.Signalled += OnMessage;

	    readyEvent = new ITC.GtkListener(debug.Ready);
	    readyEvent.Signalled += OnReady;

	    if (DebuggerStarted != null)
		DebuggerStarted(this, null);
	}

	// Request that the debugger terminate. This is an asynchronous
	// operation, and termination will not happen immediately.
	public void Terminate()
	{
	    if (debug == null)
		return;

	    debug.Cancel.Raise();
	    debug.Commands.Close();
	}

	// Send a command to the debugger.
	public void SendCommand(string command)
	{
	    if (debug == null)
		return;

	    debug.Commands.Send(command);
	    isReady = false;

	    if (DebuggerBusy != null)
		DebuggerBusy(this, null);
	}

	// Request that a running command be interrupted.
	public void SendInterrupt()
	{
	    if (debug == null)
		return;

	    debug.Cancel.Raise();
	}

	// A message has been received from the debugger, or it has
	// exited.
	void OnMessage(object sender, EventArgs args)
	{
	    Debugger.Message msg;

	    while (debug.Output.TryReceive(out msg))
		MessageReceived(this, new MessageEventArgs(msg));

	    if (debug.Output.IsClosed)
	    {
		messageEvent.Disable();
		readyEvent.Disable();
		debug = null;
		messageEvent = null;
		readyEvent = null;

		if (DebuggerExited != null)
		    DebuggerExited(this, null);
	    }
	}

	// The debugger has indicated that it's ready to receive a
	// command.
	void OnReady(object sender, EventArgs args)
	{
	    debug.Ready.Clear();
	    isReady = true;

	    if (DebuggerReady != null)
		DebuggerReady(this, null);
	}
    }
}
