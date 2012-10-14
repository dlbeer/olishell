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
using System.Threading;
using System.Threading.Tasks;

// This file defines inter-thread communication primitives. These are
// intended to make it much easier to structure asynchronous operations
// using the communication sequential processes model.

namespace Olishell
{
    // All ITC primitives have a "signalled" state, like Win32 kernel
    // objects. The signalled state indicates that some sort of
    // consumption is possible (usually). You can wait for any of a set
    // of objects to become signalled, with an optional timeout.
    abstract class ITCPrimitive
    {
	public delegate void Callback(ITCPrimitive source);

	// A listener is a one-shot thread-safe callback. It can be
	// fired multiple times, but will only ever execute once. It can
	// be associated with multiple primitives.
	class Listener
	{
	    int isFired;
	    public Callback cont;
	    volatile Timer timeout;

	    public Listener()
	    {
		isFired = 0;
	    }

	    public void Fire(ITCPrimitive source)
	    {
		if (Interlocked.Exchange(ref isFired, 1) == 0)
		{
		    if (timeout != null)
			timeout.Dispose();
		    cont(source);
		}
	    }

	    // Warning: the created timer will be referenced from the
	    // Fire() handler. Do not call this method after the listener
	    // has been attached to a primitive!
	    public void SetTimeout(int periodMs)
	    {
		timeout = new Timer((obj) => Fire(null), null,
				    periodMs, Timeout.Infinite);
	    }
	}

	// Set of registered listeners. Listeners can be registered from
	// any thread.
	object listenMutex = new object();
	HashSet<Listener> listeners = new HashSet<Listener>();

	// This property must be implemented in a derived class. The
	// derived class should also call the protected property
	// fireListeners() every time the signalled state becomes true.
	public abstract bool Signalled { get; }

	// Add the given listener, to be fired whenever the primitive
	// becomes signalled. If the primitive is already signalled, the
	// listener is fired immediately.
	void Listen(Listener l)
	{
	    lock (listenMutex)
		listeners.Add(l);

	    if (Signalled)
		fireListeners();
	}

	// Remove the given listener from the listening set. This
	// function does not guarantee that the listener won't be
	// called. It merely exists to free resources after a listener
	// has already been fired.
	void Unlisten(Listener l)
	{
	    lock (listenMutex)
		listeners.Remove(l);
	}

	// This function is called in derived classes whenever the
	// signalled state becomes true. It fires all listeners.
	protected void fireListeners()
	{
	    HashSet<Listener> newListeners = new HashSet<Listener>();
	    HashSet<Listener> ls;

	    lock (listenMutex)
	    {
		ls = listeners;
		listeners = newListeners;
	    }

	    foreach (Listener l in ls)
		l.Fire(this);
	}

	// Invoke a callback whenever any of the given primitives become
	// signalled, or when the timeout expires. The argument to the
	// callback is the primitive which became ready, or null if the
	// timeout expired.
	public static void WhenSignalled(ITCPrimitive[] prims,
					 Callback cb,
					 int timeoutMs = -1)
	{
	    Listener l = new Listener();

	    l.cont = (src) => {
		foreach (ITCPrimitive p in prims)
		    p.Unlisten(l);

		cb(src);
	    };

	    if (timeoutMs > 0)
		l.SetTimeout(timeoutMs);

	    foreach (ITCPrimitive p in prims)
		p.Listen(l);
	}

	public static void WhenSignalled(ITCPrimitive prim,
					 Callback cb,
					 int timeoutMs = -1)
	{
	    WhenSignalled(new ITCPrimitive[]{prim}, cb, timeoutMs);
	}
    }

    // The simplest of the ITC primitives is the event. It is
    // raised/cleared manually.
    class ITCEvent : ITCPrimitive
    {
	volatile bool state = false;

	public override bool Signalled
	{
	    get { return state; }
	}

	public void Raise()
	{
	    state = true;
	    fireListeners();
	}

	public void Clear()
	{
	    state = false;
	}
    }

    // A semaphore has two operations: raise and lower, and
    // holds an internal count (initially 0). The semaphore is signalled
    // whenever its count is non-negative.
    class ITCSemaphore : ITCPrimitive
    {
	object stateMutex = new object();
	int state = 0;

	public ITCSemaphore() { }
	public ITCSemaphore(int st)
	{
	    state = st;
	}

	public int State
	{
	    get
	    {
		lock (stateMutex)
		    return state;
	    }
	}

	public override bool Signalled
	{
	    get
	    {
		lock (stateMutex)
		    return state > 0;
	    }
	}

	public void Raise()
	{
	    bool fire = false;

	    lock (stateMutex)
	    {
		state++;
		fire = (state > 0);
	    }

	    if (fire)
		fireListeners();
	}

	public bool TryLower()
	{
	    bool ret = false;

	    lock (stateMutex)
		if (state > 0)
		{
		    state--;
		    ret = true;
		}

	    return ret;
	}
    }

    // This primitive is the opposite of a semaphore. It can be used to
    // keep an waitable count of outstanding operations. It contains two
    // operations: Inc() and Dec(). The primitive becomes signalled
    // when the count is 0.
    class ITCCounter : ITCPrimitive
    {
	object stateMutex = new object();
	int state = 0;

	public ITCCounter() { }

	public ITCCounter(int init)
	{
	    state = init;
	}

	public override bool Signalled
	{
	    get
	    {
		lock (stateMutex)
		    return state == 0;
	    }
	}

	public void Inc()
	{
	    lock (stateMutex)
		state++;
	}

	public void Dec()
	{
	    bool fire = false;

	    lock (stateMutex)
	    {
		state--;
		fire = (state <= 0);
	    }

	    if (fire)
		fireListeners();
	}
    }

    // A channel represents a stream of objects. On the producer side,
    // two operations are possible: Send(object) and Close(). On the
    // consumer side, only receiving is possible. The channel is
    // buffered, and closing the channel has no visible effect to the
    // consumer until the buffer empties (much like standard file
    // streams or sockets).
    //
    // A channel is signalled whenever the buffer is full, or the
    // channel is closed -- in other words, whenever there is new
    // information for the consumer.
    class ITCChannel<T> : ITCPrimitive
    {
	object stateMutex = new object();
	Queue<T> queue = new Queue<T>();
	bool closeRequested = false;

	public override bool Signalled
	{
	    get
	    {
		lock (stateMutex)
		    return (queue.Count > 0) || closeRequested;
	    }
	}

	public void Send(T msg)
	{
	    lock (stateMutex)
		if (!closeRequested)
		    queue.Enqueue(msg);

	    fireListeners();
	}

	public void Close()
	{
	    lock (stateMutex)
		closeRequested = true;

	    fireListeners();
	}

	public bool IsClosed
	{
	     get
	     {
		 lock (stateMutex)
		     return (queue.Count == 0) && closeRequested;
	     }
	}

	public bool TryReceive(out T val)
	{
	    lock (stateMutex)
		if (queue.Count > 0)
		{
		    val = queue.Dequeue();
		    return true;
		}

	    val = default(T);
	    return false;
	}
    }

    // This wrapper is a utility to produce Tasks for waiting for
    // ITC primitives.
    class ITCTask
    {
	public static Task<ITCPrimitive> WaitAsync(ITCPrimitive[] prims,
						   int timeoutMs = -1)
	{
	    var tsc = new TaskCompletionSource<ITCPrimitive>();

	    ITCPrimitive.WhenSignalled(prims,
		(src) => tsc.SetResult(src), timeoutMs);

	    return tsc.Task;
	}

	public static Task<ITCPrimitive> WaitAsync(ITCPrimitive prim,
						   int timeoutMs = -1)
	{
	    return WaitAsync(new ITCPrimitive[]{prim}, timeoutMs);
	}
    }

    // This wrapper is another utility which produces a synchronous Gtk
    // event for a single ITC primitive. Its methods should be called
    // only from the Gtk thread.
    class ITCGtk
    {
	public delegate void ITCEventHandler(object sender,
		EventArgs args);

	public event ITCEventHandler Signalled;
	public readonly ITCPrimitive Primitive;

	bool enabled = false;
	bool listening = false;

	public ITCGtk(ITCPrimitive p)
	{
	    Primitive = p;
	}

	// Enable the event. The Signalled handler will be raised
	// whenever the wrapper primitive is ready. This is a
	// level-triggered event and will continue to fire as long as
	// the primitive remains ready.
	public void Enable()
	{
	    enabled = true;
	    listen();
	}

	// Synchronously disable the event. After this call returns, no
	// further events will be raised.
	public void Disable()
	{
	    enabled = false;
	}

	// This method is invoked in the Gtk event loop when the
	// primitive becomes signalled, and we have previously listened
	// for it. The "listening" flag acts as a guard against multiple
	// scheduling of this event.
	void gtkHandler(object sender, EventArgs args)
	{
	    listening = false;

	    try {
		if (enabled)
		    Signalled(this, new EventArgs());
	    }
	    finally
	    {
		listen();
	    }
	}

	// Reschedule the Gtk handler to be run when the primitive
	// becomes ready. This is only done if we need to do this and
	// the handler hasn't already been scheduled.
	void listen()
	{
	    if (enabled && !listening)
		ITCPrimitive.WhenSignalled(Primitive, (p) =>
		    Gtk.Application.Invoke(gtkHandler));
	}
    }
}
