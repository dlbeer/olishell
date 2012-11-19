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
using System.Xml;
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
	SampleQueue		powerData;

	// Public events for state changes.
	public event EventHandler DebuggerStarted;
	public event EventHandler DebuggerReady;
	public event EventHandler DebuggerBusy;
	public event EventHandler DebuggerExited;
	public event MessageEventHandler MessageReceived;
	public event EventHandler PowerChanged;

	public DebugManager(Settings set)
	{
	    settings = set;
	}

	// Fetch power sample data
	public SampleQueue PowerData
	{
	    get { return powerData; }
	}

	// Clear power graph
	public void ClearPower()
	{
	    if (powerData != null)
	    {
		powerData.Clear();
		if (PowerChanged != null)
		    PowerChanged(this, null);
	    }
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
	    Start(settings.MSPDebugArgs);
	}

	// Start a new debugger process if the debugger is not already
	// running.
	public void Start(string cmdline)
	{
	    if (debug != null)
		return;

	    string path = settings.MSPDebugPath;
	    string args = "--embed " + cmdline;

	    if (settings.UseBundledDebugger)
	    {
		string self = Assembly.GetExecutingAssembly().Location;

		path = Path.Combine
		    (Path.GetDirectoryName(self), "mspdebug.exe");
	    }

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
	    {
		switch (msg.Type)
		{
		case Debugger.MessageType.Normal:
		case Debugger.MessageType.Debug:
		    Console.Out.WriteLine(msg.Text);
		    break;

		case Debugger.MessageType.Error:
		    Console.Error.WriteLine(msg.Text);
		    break;

		case Debugger.MessageType.Shell:
		    break;
		}

		if (msg.Type == Debugger.MessageType.Shell)
		    HandleShell(msg);
		else if (MessageReceived != null)
		    MessageReceived(this, new MessageEventArgs(msg));
	    }

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

	// Process a shell message
	void HandleShell(Debugger.Message msg)
	{
	    int spc = msg.Text.IndexOf(' ');

	    if (spc < 0)
		return;

	    string kind = msg.Text.Substring(0, spc);
	    string arg = msg.Text.Substring(spc + 1,
		msg.Text.Length - spc - 1);

	    if (kind.Equals("power-sample-us"))
	    {
		int period = XmlConvert.ToInt32(arg);

		powerData = new SampleQueue(period, 131072);
		if (PowerChanged != null)
		    PowerChanged(this, null);
	    }
	    else if (kind.Equals("power-samples"))
	    {
		if (powerData != null)
		{
		    int[] samples = DecodeSamples(arg);

		    powerData.Push(samples);
		    if (PowerChanged != null)
			PowerChanged(this, null);
		}
	    }
	}

	// Decode power sample data into an array of microamp samples.
	static int[] DecodeSamples(string encoded)
	{
	    byte[] raw = Convert.FromBase64String(encoded);
	    int count = 0;

	    for (int i = 3; i < raw.Length; i += 4)
		if ((raw[i] & 0x80) == 0)
		    count++;

	    int[] samples = new int[count];

	    count = 0;
	    for (int i = 0; i + 3 < raw.Length; i += 4)
		if ((raw[i + 3] & 0x80) == 0)
		    samples[count++] =
			((int)raw[i]) |
			(((int)raw[i + 1]) << 8) |
			(((int)raw[i + 2]) << 16) |
			(((int)raw[i + 3]) << 24);

	    return samples;
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
