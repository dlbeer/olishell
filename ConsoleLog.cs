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
    // Console log view. This acts as a simple ANSI terminal without
    // cursor control. Lines of text may be added, and embedded ANSI
    // escape sequences will be decoded and used to colourize the added
    // text.
    class ConsoleLog
    {
	TextView textView;
	ScrolledWindow scroller;
	TextTag[] tagTab;
	Settings settings;

	public Widget View
	{
	    get { return scroller; }
	}

	public ConsoleLog(Settings set)
	{
	    settings = set;

	    textView = new TextView();
	    textView.Editable = false;
	    textView.CursorVisible = false;

	    scroller = new ScrolledWindow();
	    scroller.SetPolicy(PolicyType.Automatic, PolicyType.Always);
	    scroller.Add(textView);

	    InitTags();

	    settings.RefreshFont += OnRefreshFont;
	    OnRefreshFont(this, null);
	}

	void OnRefreshFont(object sender, EventArgs args)
	{
	    try
	    {
		var font = Pango.FontDescription.FromString
			(settings.ConsoleFont);

		textView.ModifyFont(font);
	    }
	    catch (Exception) { }
	}

	// Set up tags corresponding to each ANSI foreground state.
	void InitTags()
	{
	    int i;

	    tagTab = new TextTag[16];
	    for (i = 0; i < 16; i++)
	    {
		string name = Convert.ToString(i);
		TextTag tag = new TextTag(name);
		bool bold = (i & 8) != 0;
		byte low = (byte)(bold ? 0x60 : 0x00);
		byte high = (byte)(bold ? 0xff : 0xa0);
		byte r = ((i & 1) == 0) ? low : high;
		byte g = ((i & 2) == 0) ? low : high;
		byte b = ((i & 4) == 0) ? low : high;

		if ((i & 7) != 7)
		    tag.ForegroundGdk = new Gdk.Color(r, g, b);
		if (bold)
		    tag.Weight = Pango.Weight.Bold;

		textView.Buffer.TagTable.Add(tag);
		tagTab[i] = tag;
	    }
	}

	// Clear the console
	public void Clear()
	{
	    textView.Buffer.Clear();
	}

	// Add a line of text to the console window, possibly containing
	// embedded ANSI codes.
	public void AddLine(string text)
	{
	    TextBuffer buf = textView.Buffer;
	    TextIter iter = buf.EndIter;
	    int ansiState = 7;
	    TextTag[] tagBox = new TextTag[1];
	    int i = 0;

	    while (i < text.Length)
	    {
		if (text[i] == 0x1b)
		{
		    i += ParseANSI(text, i, ref ansiState);
		}
		else
		{
		    int next = text.IndexOf((char)0x1b, i);

		    if (next < 0)
			next = text.Length;

		    tagBox[0] = tagTab[ansiState & 0xf];
		    buf.InsertWithTags(ref iter, text.Substring(i, next - i),
			   tagBox);
		    i = next;
		}
	    }

	    buf.Insert(ref iter, "\n");
	    textView.ScrollMarkOnscreen(buf.InsertMark);
	}

	// Given a string containing an ANSI code at the given offset,
	// return the length of the code and update the old ANSI state
	// to the new state based on the code's content.
	static int ParseANSI(string text, int offset, ref int state)
	{
	    int next = state;
	    int code = 0;
	    int len = 0;

	    while (len < text.Length)
	    {
		char c = text[offset + len];

		len++;

		if (c >= '0' && c <= '9')
		{
		    code = code * 10 + c - '0';
		}
		else
		{
		    next = ApplyANSI(next, code);
		    code = 0;
		}

		if (c == 'm')
		    break;
	    }

	    state = next;
	    return len;
	}

	// Apply an ANSI colour code fragment to the given state and
	// return the new state.
	static int ApplyANSI(int state, int code)
	{
	    // 0: reset
	    if (code == 0)
		return 7;

	    // 1: bold
	    if (code == 1)
		return state | 0x8;

	    // 30-37: foreground colour
	    if (code >= 30 && code <= 37)
		return (state & 0xf8) | (code - 30);

	    return state;
	}

	// Select all text
	public void SelectAll()
	{
	    TextBuffer buf = textView.Buffer;

	    buf.SelectRange(buf.StartIter, buf.EndIter);
	}

	// Copy selected text
	public void CopyText()
	{
	    TextBuffer buf = textView.Buffer;

	    buf.CopyClipboard(Clipboard.Get(Gdk.Selection.Clipboard));
	}

	// Retrieve full transcript
	public string Transcript
	{
	    get { return textView.Buffer.Text; }
	}
    }
}
