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
using System.Collections.Generic;
using Gtk;

namespace Olishell
{
    class AppToolbar
    {
	Toolbar			toolBar;

	DebugManager		debugManager;
	DebugPane		debugPane;

	ToolButton		debuggerStart;
	ToolButton		debuggerStop;
	ToolButton		debuggerInterrupt;

	List<ToolButton>	commandMacros = new List<ToolButton>();

	public AppToolbar(DebugManager mgr, DebugPane dpane)
	{
	    debugManager = mgr;
	    debugPane = dpane;
	    toolBar = new Toolbar();

	    // Debugger control buttons
	    debuggerStart = new ToolButton(Stock.MediaPlay);
	    debuggerStart.Clicked += OnDebuggerStart;
	    debuggerStart.Label = "Start";
	    debuggerStart.TooltipText = "Start debugger";
	    toolBar.Add(debuggerStart);

	    debuggerStop = new ToolButton(Stock.MediaStop);
	    debuggerStop.Clicked += OnDebuggerStop;
	    debuggerStop.Label = "Stop";
	    debuggerStop.TooltipText = "Stop debugger";
	    toolBar.Add(debuggerStop);

	    debuggerInterrupt = new ToolButton(Stock.Cancel);
	    debuggerInterrupt.Clicked += OnDebuggerInterrupt;
	    debuggerInterrupt.Label = "Interrupt";
	    debuggerInterrupt.TooltipText = "Interrupt debugger";
	    toolBar.Add(debuggerInterrupt);

	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;

	    toolBar.Add(new SeparatorToolItem());

	    // Command macros
	    var cmdProg = new ToolButton(Stock.Open);
	    cmdProg.Clicked += OnCommandProgram;
	    cmdProg.Label = "Program";
	    cmdProg.TooltipText = "Program...";
	    toolBar.Add(cmdProg);
	    commandMacros.Add(cmdProg);

	    var cmdReset = new ToolButton(Stock.Clear);
	    cmdReset.Clicked += (obj, evt) =>
		debugPane.DebugView.RunCommand("reset");
	    cmdReset.Label = "Reset";
	    cmdReset.TooltipText = "Reset";
	    toolBar.Add(cmdReset);
	    commandMacros.Add(cmdReset);

	    var cmdRun = new ToolButton(Stock.GoForward);
	    cmdRun.Clicked += (obj, evt) =>
		debugPane.DebugView.RunCommand("run");
	    cmdRun.Label = "Run";
	    cmdRun.TooltipText = "Run";
	    toolBar.Add(cmdRun);
	    commandMacros.Add(cmdRun);

	    var cmdStep = new ToolButton(Stock.MediaNext);
	    cmdStep.Clicked += (obj, evt) =>
		debugPane.DebugView.RunCommand("step");
	    cmdStep.Label = "Step";
	    cmdStep.TooltipText = "Step";
	    toolBar.Add(cmdStep);
	    commandMacros.Add(cmdStep);

	    foreach (ToolButton m in commandMacros)
		m.Sensitive = false;

	    toolBar.Add(new SeparatorToolItem());

	    // Zoom controls
	    var zoomIn = new ToolButton(Stock.ZoomIn);
	    zoomIn.Clicked += (obj, evt) => debugPane.PowerView.ZoomIn();
	    zoomIn.Label = "Zoom in";
	    zoomIn.TooltipText = "Zoom in";
	    toolBar.Add(zoomIn);

	    var zoomOut = new ToolButton(Stock.ZoomOut);
	    zoomOut.Clicked += (obj, evt) => debugPane.PowerView.ZoomOut();
	    zoomOut.Label = "Zoom out";
	    zoomOut.TooltipText = "Zoom out";
	    toolBar.Add(zoomOut);

	    var zoomFit = new ToolButton(Stock.ZoomFit);
	    zoomFit.Clicked += (obj, evt) => debugPane.PowerView.ZoomFit();
	    zoomFit.Label = "Zoom fit";
	    zoomFit.TooltipText = "Zoom to fit";
	    toolBar.Add(zoomFit);

	    var zoomFull = new ToolButton(Stock.Zoom100);
	    zoomFull.Clicked += (obj, evt) => debugPane.PowerView.ZoomFull();
	    zoomFull.Label = "Zoom full";
	    zoomFull.TooltipText = "Zoom full";
	    toolBar.Add(zoomFull);

	    // Debug manager listeners
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

	void OnCommandProgram(object sender, EventArgs args)
	{
	    FileChooserDialog dlg = new FileChooserDialog("Program file",
		null, FileChooserAction.Open,
		new object[]{Stock.Cancel, ResponseType.Cancel,
			     Stock.Ok, ResponseType.Ok});

	    dlg.DefaultResponse = ResponseType.Ok;

	    if ((ResponseType)dlg.Run() == ResponseType.Ok)
		debugPane.DebugView.RunCommand("prog " + dlg.Filename);

	    dlg.Hide();
	}

	void OnDebuggerBusy(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = true;

	    foreach (ToolButton m in commandMacros)
		m.Sensitive = false;
	}

	void OnDebuggerReady(object sender, EventArgs args)
	{
	    debuggerInterrupt.Sensitive = false;

	    foreach (ToolButton m in commandMacros)
		m.Sensitive = true;
	}

	void OnDebuggerStarted(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = false;
	    debuggerStop.Sensitive = true;
	    debuggerInterrupt.Sensitive = true;

	    foreach (ToolButton m in commandMacros)
		m.Sensitive = false;
	}

	void OnDebuggerExited(object sender, EventArgs args)
	{
	    debuggerStart.Sensitive = true;
	    debuggerStop.Sensitive = false;
	    debuggerInterrupt.Sensitive = false;

	    foreach (ToolButton m in commandMacros)
		m.Sensitive = false;
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
