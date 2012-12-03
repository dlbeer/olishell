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
    class AppToolbar
    {
	Toolbar			toolBar;

	DebugManager		debugManager;

	ToolButton		debuggerStart;
	ToolButton		debuggerStop;
	ToolButton		debuggerInterrupt;

	public AppToolbar(DebugManager mgr)
	{
	    debugManager = mgr;
	    toolBar = new Toolbar();

	    debuggerStart = new ToolButton(Stock.MediaPlay);
	    debuggerStart.Clicked += OnDebuggerStart;
	    debuggerStart.TooltipText = "Start debugger";
	    toolBar.Add(debuggerStart);

	    debuggerStop = new ToolButton(Stock.MediaStop);
	    debuggerStop.Clicked += OnDebuggerStop;
	    debuggerStop.TooltipText = "Stop debugger";
	    toolBar.Add(debuggerStop);

	    debuggerInterrupt = new ToolButton(Stock.Cancel);
	    debuggerInterrupt.Clicked += OnDebuggerInterrupt;
	    debuggerInterrupt.TooltipText = "Interrupt debugger";
	    toolBar.Add(debuggerInterrupt);

	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;

	    debugManager.DebuggerBusy += OnDebuggerBusy;
	    debugManager.DebuggerReady += OnDebuggerReady;
	    debugManager.DebuggerStarted += OnDebuggerStarted;
	    debugManager.DebuggerExited += OnDebuggerExited;
	}

	public Widget View
	{
	    get { return toolBar; }
	}

	void OnDestroy(object sender, EventArgs args)
	{
	    debugManager.DebuggerBusy -= OnDebuggerBusy;
	    debugManager.DebuggerReady -= OnDebuggerReady;
	    debugManager.DebuggerStarted -= OnDebuggerStarted;
	    debugManager.DebuggerExited -= OnDebuggerExited;
	}

	void OnDebuggerBusy(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = true;
	}

	void OnDebuggerReady(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = false;
	}

	void OnDebuggerStarted(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = false;
	    debuggerStop.Sensitive = true;
	    debuggerInterrupt.Sensitive = true;
	}

	void OnDebuggerExited(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;
	}

	void OnDebuggerStart(object sender, EventArgs args)
	{
	    debugManager.Start();
	}

	void OnDebuggerStop(object sender, EventArgs args)
	{
	    debugManager.Terminate();
	}

	void OnDebuggerInterrupt(object sender, EventArgs args)
	{
	    debugManager.SendInterrupt();
	}

    }
}
