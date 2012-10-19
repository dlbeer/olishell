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
using System.Linq;

namespace Olishell
{
    // This object is a data structure holding power sample data. Each
    // sample is represented by an int, representing the current draw in
    // microamps.
    class SampleQueue
    {
	// Sample period, in microseconds
	public readonly int Period;

	// Rolling sample buffer
	int[]	samples;
	int	count;
	int	start;
	int	maxValue;

	public SampleQueue(int period, int maxLen)
	{
	    Period = period;
	    count = 0;
	    start = 0;
	    maxValue = 0;
	    samples = new int[maxLen];
	}

	// Retrieve the number of samples currently in the buffer.
	public int Count
	{
	    get { return count; }
	}

	// Retrieve the maximum value in the sample set.
	public int Max
	{
	    get { return maxValue; }
	}

	// Clear the buffer.
	public void Clear()
	{
	    count = 0;
	    start = 0;
	    maxValue = 0;
	}

	// Add new samples to the buffer, possibly pushing out old ones.
	public void Push(int[] incoming)
	{
	    int head = (start + count) % samples.Length;
	    int max = incoming.Max();

	    if (max > maxValue)
		maxValue = max;

	    for (int i = 0; i < incoming.Length; i++)
	    {
		samples[head] = incoming[i];
		head = (head + 1) % samples.Length;
	    }

	    // Roll the buffer
	    count += incoming.Length;
	    if (count > samples.Length)
	    {
		start += count % samples.Length;
		count = samples.Length;
	    }
	}

	// Fetch data from the queue into the sample buffer. The scale
	// parameter gives a downsampling ratio, but the offset is
	// always specified in raw samples.
	//
	// Returns the number of entries written in the outgoing buffer.
	public int Fetch(int offset, int[] outgoing, int scale = 1)
	{
	    int len = outgoing.Length;

	    if (offset >= count)
		return 0;

	    if (offset + len * scale > count)
		len = (count - offset) / scale;

	    offset += start;

	    for (int i = 0; i < len; i++)
		outgoing[i] = 0;

	    for (int n = 0; n < scale; n++)
		for (int i = 0; i < len; i++)
		    outgoing[i] +=
			samples[(offset + i * scale) % samples.Length];

	    for (int i = 0; i < len; i++)
		outgoing[i] /= scale;

	    return len;
	}
    }
}
