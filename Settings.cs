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
using System.Xml.Serialization;

namespace Olishell
{
    public class Settings
    {
	public bool PowerGraphVisible = true;
	public int SizerPosition = 200;
	public int WindowWidth = 700;
	public int WindowHeight = 500;

	public bool UseBundledDebugger = true;
	public string MSPDebugPath = "";
	public string MSPDebugArgs = "olimex-iso-mk2";

	public event EventHandler RefreshLayout;

	public Settings() { }

	public void RaiseRefreshLayout()
	{
	    if (RefreshLayout != null)
		RefreshLayout(this, null);
	}

	public void Save()
	{
	    XmlSerializer xs = new XmlSerializer(typeof(Settings));

	    using (Stream s = File.Create(DocumentPath()))
		xs.Serialize(s, this);
	}

	public static Settings Load()
	{
	    try
	    {
		XmlSerializer xs = new XmlSerializer(typeof(Settings));

		using (Stream s = File.Open(DocumentPath(), FileMode.Open))
		    return (Settings)xs.Deserialize(s);
	    }
	    catch (Exception) { }

	    return new Settings();
	}

	static string DocumentPath()
	{
	    return Path.Combine(Environment.GetFolderPath
		(Environment.SpecialFolder.ApplicationData),
		    "olishell-settings.xml");
	}
    }
}
