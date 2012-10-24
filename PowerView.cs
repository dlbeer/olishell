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
	int		scale = 128;
	int		vertMax = 10;
	int		hSpacingUs = 1;
	Gdk.Rectangle	allocation;
	Gdk.Rectangle	scrollAllocation;
	uint		timerID;
	Settings	settings;

	Gdk.GC		gcBar;
	Gdk.GC		gcGrid;

	// Periodicity of the gcGrid dotted line pattern
	const int	dotsPeriod = 6;
	const int	maxScale = 65536;

	public PowerView(Settings set, DebugManager mgr)
	{
	    settings = set;
	    debugManager = mgr;

	    scale = FixScale(settings.PowerViewScale);

	    drawer.ExposeEvent += OnExpose;
	    drawer.SizeAllocated += OnSizeAllocate;
	    drawer.Realized += OnRealize;

	    scroll.AddWithViewport(drawer);
	    scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
	    scroll.SizeAllocated += OnScrollSizeAllocate;

	    scroll.Destroyed += OnDestroy;
	    debugManager.PowerChanged += OnPowerChanged;
	}

	static int FixScale(int target)
	{
	    int s = 1;

	    while ((s < target) && (s < maxScale))
		s *= 2;

	    return s;
	}

	public void SaveLayout()
	{
	    settings.PowerViewScale = scale;
	}

	void OnDestroy(object sender, EventArgs args)
	{
	    debugManager.PowerChanged -= OnPowerChanged;

	    if (timerID != 0)
	    {
		GLib.Source.Remove(timerID);
		timerID = 0;
	    }
	}

	void OnPowerChanged(object sender, EventArgs args)
	{
	    if (timerID == 0)
		timerID = GLib.Timeout.Add(100, OnTimer);
	}

	bool OnTimer()
	{
	    timerID = 0;
	    updateSizing();
	    return false;
	}

	public Widget View
	{
	    get { return scroll; }
	}

	public void ZoomIn()
	{
	    if (scale > 1)
	    {
		scale /= 2;
		updateSizing();
	    }
	}

	public void ZoomOut()
	{
	    if (scale < maxScale)
	    {
		scale *= 2;
		updateSizing();
	    }
	}

	public void ZoomFit()
	{
	    SampleQueue data = debugManager.PowerData;

	    if (data == null)
	    {
		scale = 1;
	    }
	    else
	    {
		scale = 1;
		while ((scale < maxScale) &&
		       (scrollAllocation.Width * scale) < data.Count)
		    scale *= 2;
	    }

	    updateSizing();
	}

	public void ZoomFull()
	{
	    scale = 1;
	    updateSizing();
	}

	void updateSizing()
	{
	    SampleQueue data = debugManager.PowerData;

	    vertMax = 10;

	    if (data != null)
	    {
		drawer.SetSizeRequest(data.Count / scale, -1);

		// Calculate a scale for the vertical axis.
		while (vertMax < data.Max)
		    vertMax *= 10;

		// Calculate a good time-division spacing
		int usPerPx = scale * data.Period;

		hSpacingUs = 1;
		while (hSpacingUs / usPerPx < 20)
		    hSpacingUs *= 10;
	    }

	    drawer.QueueResize();
	}

	void OnSizeAllocate(object sender, SizeAllocatedArgs args)
	{
	    allocation = args.Allocation;
	}

	void OnScrollSizeAllocate(object sender, SizeAllocatedArgs args)
	{
	    scrollAllocation = args.Allocation;
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
	    Gdk.Rectangle rect = args.Event.Area;
	    Gdk.Window win = drawer.GdkWindow;

	    win.DrawRectangle(drawer.Style.BlackGC, true,
			      rect.X, 0, rect.Width, allocation.Height);

	    DrawPower(win, rect);
	    DrawHorizontalGrid(win, rect);
	    DrawVerticalGrid(win, rect);
	}

	// Redraw power samples for the exposed area.
	void DrawPower(Gdk.Window win, Gdk.Rectangle rect)
	{
	    SampleQueue data = debugManager.PowerData;

	    if (data == null)
		return;

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

	// Redraw horizontal gridlines for the exposed area.
	void DrawHorizontalGrid(Gdk.Window win, Gdk.Rectangle rect)
	{
	    int x1 = rect.X;
	    int x2 = rect.X + rect.Width - 1;

	    // Align the horizontal region so the dotted pattern always
	    // appears continuous.
	    x1 -= x1 % dotsPeriod;
	    x2 += dotsPeriod - (x2 % dotsPeriod);

	    for (int i = 1; i < 10; i++) {
		int y = allocation.Height * i / 10;

		win.DrawLine(gcGrid, x1, y, x2, y);
	    }
	}

	// Redraw vertical gridlines for the exposed area.
	void DrawVerticalGrid(Gdk.Window win, Gdk.Rectangle rect)
	{
	    SampleQueue data = debugManager.PowerData;

	    if (data == null)
		return;

	    int usPerPx = scale * data.Period;
	    int t = rect.X * usPerPx;

	    // Find the first time divison before the exposed area
	    t -= t % hSpacingUs;

	    for (;;)
	    {
		int x = t / usPerPx;

		if (x >= rect.X + rect.Width)
		    break;

		win.DrawLine(gcGrid, x, 0, x, allocation.Height - 1);
		t += hSpacingUs;
	    }
	}
    }
}
