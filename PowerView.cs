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
    class PowerView
    {
	ScrolledWindow	scroll = new ScrolledWindow();
	DrawingArea	drawer = new DrawingArea();
	DebugManager	debugManager;
	int		scale = 1;
	int		vertMax = 10;
	Gdk.Rectangle	allocation;

	Gdk.GC		gcBar;
	Gdk.GC		gcGrid;

	public PowerView(DebugManager mgr)
	{
	    debugManager = mgr;

	    drawer.ExposeEvent += OnExpose;
	    drawer.SizeAllocated += OnSizeAllocate;
	    drawer.Realized += OnRealize;

	    scroll.AddWithViewport(drawer);
	    scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

	    scroll.Destroyed += OnDestroy;
	    debugManager.PowerChanged += OnPowerChanged;
	}

	void OnDestroy(object sender, EventArgs args)
	{
	    debugManager.PowerChanged -= OnPowerChanged;
	}

	void OnPowerChanged(object sender, EventArgs args)
	{
	    updateSizing();
	}

	public Widget View
	{
	    get { return scroll; }
	}

	public int Scale
	{
	    get { return scale; }
	    set
	    {
		scale = value;
		updateSizing();
	    }
	}

	void updateSizing()
	{
	    SampleQueue data = debugManager.PowerData;

	    vertMax = 10;

	    if (data != null)
	    {
		drawer.SetSizeRequest(data.Count / scale, -1);

		while (vertMax < data.Max)
		    vertMax *= 10;
	    }

	    drawer.QueueResize();
	}

	void OnSizeAllocate(object sender, SizeAllocatedArgs args)
	{
	    allocation = args.Allocation;
	}

	void OnRealize(object sender, EventArgs args)
	{
	    gcBar = new Gdk.GC(drawer.GdkWindow);
	    gcBar.RgbFgColor = new Gdk.Color(0xa0, 0, 0);

	    gcGrid = new Gdk.GC(drawer.GdkWindow);
	    gcGrid.RgbFgColor = new Gdk.Color(0x60, 0x60, 0xff);
	    gcGrid.SetLineAttributes(1,
		Gdk.LineStyle.OnOffDash,
		Gdk.CapStyle.Butt,
		Gdk.JoinStyle.Miter);
	    gcGrid.SetDashes(0, new sbyte[]{2, 4}, 2);
	}

	void OnExpose(object sender, ExposeEventArgs args)
	{
	    SampleQueue data = debugManager.PowerData;
	    Gdk.Rectangle rect = args.Event.Area;
	    Gdk.Window win = drawer.GdkWindow;

	    win.DrawRectangle(drawer.Style.BlackGC, true,
			      rect.X, 0, rect.Width, allocation.Height);

	    if (data != null)
	    {
		int[] slice = new int[rect.Width];
		int len;

		len = data.Fetch(rect.X * scale, slice, scale);

		for (int i = 0; i < len; i++)
		{
		    int x = rect.X + i;
		    int h = slice[i] * allocation.Height / vertMax;

		    win.DrawLine(gcBar, x, allocation.Height - 1 - h,
				 x, allocation.Height - 1);
		}
	    }

	    for (int i = 1; i < 10; i++) {
		int y = allocation.Height * i / 10;

		win.DrawLine(gcGrid, rect.X, y, rect.X + rect.Width - 1, y);
	    }
	}
    }
}
