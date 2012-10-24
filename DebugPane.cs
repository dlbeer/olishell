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
    class DebugPane
    {
	bool isPaned = true;
	VBox top = new VBox();
	VPaned pane = new VPaned();
	PowerView powerView;
	DebugView debugView;
	Settings settings;

	public DebugPane(Settings set, DebugManager mgr)
	{
	    settings = set;
	    powerView = new PowerView(mgr);
	    debugView = new DebugView(set, mgr);
	    isPaned = settings.PowerGraphVisible;
	    SetupLayout();

	    settings.RefreshLayout += OnRefreshLayout;
	}

	public Widget View
	{
	    get { return top; }
	}

	public PowerView PowerView
	{
	    get { return powerView; }
	}

	public DebugView DebugView
	{
	    get { return debugView; }
	}

	void OnRefreshLayout(object sender, EventArgs args)
	{
	    TeardownLayout();
	    isPaned = settings.PowerGraphVisible;
	    SetupLayout();
	}

	public void SaveLayout()
	{
	    settings.SizerPosition = pane.Position;
	}

	void TeardownLayout()
	{
	    SaveLayout();

	    if (isPaned)
	    {
		pane.Remove(powerView.View);
		pane.Remove(debugView.View);
		top.Remove(pane);
	    }
	    else
	    {
		top.Remove(debugView.View);
	    }
	}

	void SetupLayout()
	{
	    if (isPaned)
	    {
		pane.Add(powerView.View);
		pane.Add(debugView.View);
		top.Add(pane);
		pane.Position = settings.SizerPosition;
	    }
	    else
	    {
		top.Add(debugView.View);
	    }

	    top.ShowAll();
	}
    }
}
