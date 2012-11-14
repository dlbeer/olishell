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
using System.IO;
using System.Diagnostics;

namespace Olishell
{
    class Debugger
    {
	public enum MessageType
	{
	    Normal = ':',
	    Debug = '-',
	    Error = '!',
	    Shell = '\\'
	}

	public class Message
	{
	    public readonly MessageType Type;
	    public readonly string Text;

	    public Message(MessageType mt, string txt)
	    {
		Type = mt;
		Text = txt;
	    }
	}

	// This is the only public interface to a running debugger
	// manager. Communication is solely via ITC. channels. Debugger
	// shutdown is requested by closing the input channel.
	public readonly ITC.Channel<Message> Output = new ITC.Channel<Message>();

	// This event becomes signalled when the debugger is ready to
	// accept a command.
	public readonly ITC.Event Ready = new ITC.Event();

	// Commands are sent to the debugger via this channel.
	public readonly ITC.Channel<string> Commands = new ITC.Channel<string>();

	// Cancellation of a running command is requested by raising
	// this semaphore. If you need to avoid race conditions for a
	// command which might either be cancelled or finish of its own
	// accord, the CancelAccepted event is raised when a
	// cancellation request is received. You can then cancel
	// synchronously as follows:
	//
	//    - Raise Cancel
	//    - Wait for CancelAccepted
	//    - Wait for Ready
	//
	// Cancellation requests are accepted and ignored when the
	// manager process is ready for a command. The CancelAccepted
	// event is still raised.
	public readonly ITC.Event Cancel = new ITC.Event();
	public readonly ITC.Event CancelAccepted = new ITC.Event();

	// Clients don't speak directly to the debugger process. The
	// intermediate debugger task uses these channels to communicate
	// with the actual process.
	ITC.Counter streamCount = new ITC.Counter(2);
	ITC.Channel<Message> rawOutput = new ITC.Channel<Message>();
	StreamWriter rawInput;
	Process proc;

	// Start an mspdebug subprocess and three logical asynchronous
	// processes:
	//
	//    - a process which reads from mspdebug's stderr and places
	//      error messages in the rawOutput channel.
	//    - a process which reads from mspdebug's stdout, decodes
	//      messages and sends them to the rawOutput channel. This
	//      process closes rawOutput when stdout closes.
	//    - a management process which forwards messages from
	//      rawOutput to Output, as well as forwarding from Input to
	//      rawInput. This process will also ensure that when a
	//      command is sent to Input, we block further requests
	//      until processing has started.
	public Debugger(string path, string args)
	{
	    var info = new ProcessStartInfo();

	    info.RedirectStandardOutput = true;
	    info.RedirectStandardInput = true;
	    info.RedirectStandardError = true;
	    info.UseShellExecute = false;
	    info.CreateNoWindow = true;
	    info.FileName = path;
	    info.Arguments = args;

	    proc = new Process();

	    proc.StartInfo = info;
	    proc.OutputDataReceived += this.OnOutputData;
	    proc.ErrorDataReceived += this.OnErrorData;

	    proc.Start();
	    rawInput = proc.StandardInput;
	    proc.BeginOutputReadLine();
	    proc.BeginErrorReadLine();

	    ITC.Pool.Continue(streamCount, CleanProcess);
	    ManagerBusy(null);
	}

	// Clean up when the process exits.
	void CleanProcess(ITC.Primitive prim)
	{
	    rawOutput.Close();
	    proc.WaitForExit();
	}

	// Shift data from stderr to the rawOutput channel, where it'll
	// be read by the management process.
	void OnErrorData(object sender, DataReceivedEventArgs args)
	{
	    string data = args.Data;

	    if (data == null)
		streamCount.Dec();
	    else
		rawOutput.Send(new Message(MessageType.Error, args.Data));
	}

	// Shift data from stdout to the rawOutput channel for the
	// manager process.
	void OnOutputData(object sender, DataReceivedEventArgs args)
	{
	    string data = args.Data;

	    if (data == null)
		streamCount.Dec();
	    else
		rawOutput.Send(OutputToMessage(data));
	}

	// Convert an embedded mode message to a structured message
	// object, based on the initial sigil.
	static Message OutputToMessage(string text)
	{
	    if (text.Length <= 0)
		return new Message(MessageType.Normal, "");

	    string rem = text.Substring(1, text.Length - 1);

	    if (text[0] == ':')
		return new Message(MessageType.Normal, rem);
	    else if (text[0] == '-')
		return new Message(MessageType.Debug, rem);
	    else if (text[0] == '!')
		return new Message(MessageType.Error, rem);
	    else if (text[0] == '\\')
		return new Message(MessageType.Shell, rem);

	    return new Message(MessageType.Normal, text);
	}

	// Manager: busy state. A command, or startup is in progress. We
	// move to the ready state once the command is finished.
	void ManagerBusy(ITC.Primitive source)
	{
	    Message msg;

	    if (rawOutput.IsClosed)
	    {
		Output.Close();
		return;
	    }

	    if (Cancel.Signalled)
	    {
		try {
		    rawInput.Write("\\break\n");
		}
		catch (Exception) { }

		Cancel.Clear();
		CancelAccepted.Raise();
	    }

	    if (rawOutput.TryReceive(out msg))
	    {
		Output.Send(msg);

		if ((msg.Type == MessageType.Shell) &&
			(msg.Text.Equals("ready")))
		{
		    Ready.Raise();
		    ManagerReady(null);
		    return;
		}
	    }

	    ITC.Pool.Continue(new ITC.Primitive[]{Cancel, rawOutput},
		this.ManagerBusy);
	}

	// Manager: ready state. mspdebug is sitting idle and waiting
	// for a command.
	void ManagerReady(ITC.Primitive source)
	{
	    string cmd;

	    if (Commands.IsClosed)
	    {
		try
		{
		    rawInput.Close();
		}
		catch (Exception) { }

		ManagerExiting(null);
		return;
	    }

	    if (Commands.TryReceive(out cmd))
	    {
		try
		{
		    rawInput.Write(":" + cmd + "\n");
		}
		catch (Exception) { }

		ManagerSubmitting(null);
		return;
	    }

	    if (Cancel.Signalled)
	    {
		Cancel.Clear();
		CancelAccepted.Raise();
		Ready.Raise();
	    }

	    ITC.Pool.Continue(new ITC.Primitive[]{Cancel, Commands},
		this.ManagerReady);
	}

	// Manager: submitting state. We've just been given a command
	// and have sent it to mspdebug. Wait for mspdebug's
	// acknowledgement of receipt before doing anything further,
	// otherwise any cancellation requests we might want to send
	// could get lost without effect.
	void ManagerSubmitting(ITC.Primitive source)
	{
	    Message msg;

	    if (rawOutput.IsClosed)
	    {
		Output.Close();
		return;
	    }

	    if (rawOutput.TryReceive(out msg))
	    {
		Output.Send(msg);
		if ((msg.Type == MessageType.Shell) &&
			(msg.Text.Equals("busy")))
		{
		    ManagerBusy(null);
		    return;
		}
	    }

	    ITC.Pool.Continue(rawOutput, this.ManagerSubmitting);
	}

	// We've closed mspdebug's stdin and are now waiting for process
	// exit.
	void ManagerExiting(ITC.Primitive source)
	{
	    Message msg;

	    if (rawOutput.IsClosed)
	    {
		Output.Close();
		return;
	    }

	    if (rawOutput.TryReceive(out msg))
		Output.Send(msg);

	    ITC.Pool.Continue(rawOutput, this.ManagerExiting);
	}
    }
}
