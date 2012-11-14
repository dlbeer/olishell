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

namespace Olishell.ITC
{
    // All primitives have a "signalled" state, like Win32 kernel
    // objects. The signalled state indicates that some sort of
    // consumption is possible (usually). You can wait for any of a set
    // of objects to become signalled, with an optional timeout.
    //
    // A primitive may have only a single consumer/waiter.
    abstract class Primitive
    {
	public delegate void Callback(Primitive source);

	// A listener is a one-shot thread-safe callback. It can be
	// fired multiple times, but will only ever execute once. It can
	// be associated with multiple primitives.
	class Listener
	{
	    int isFired;
	    public Callback cont;
	    Timer timeout;

	    public Listener()
	    {
		isFired = 0;
	    }

	    public void Fire(Primitive source)
	    {
		if (Interlocked.Exchange(ref isFired, 1) == 0)
		{
		    Timer t = Interlocked.Exchange(ref timeout, null);

		    if (t != null)
			t.Dispose();

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
	Listener listener = null;

	// This property must be implemented in a derived class. The
	// derived class should also call the protected property
	// fireListener() every time the signalled state becomes true.
	public abstract bool Signalled { get; }

	// Add the given listener, to be fired whenever the primitive
	// becomes signalled. If the primitive is already signalled, the
	// listener is fired immediately.
	void Listen(Listener l)
	{
	    listener = l;

	    if (Signalled)
		fireListener();
	}

	// Remove the given listener from the listening set. This
	// function does not guarantee that the listener won't be
	// called. It merely exists to free resources after a listener
	// has already been fired.
	void Unlisten()
	{
	    listener = null;
	}

	// This function is called in derived classes whenever the
	// signalled state becomes true. It fires all listeners.
	protected void fireListener()
	{
	    Listener l = Interlocked.Exchange(ref listener, null);

	    if (l != null)
		l.Fire(this);
	}

	// Invoke a callback whenever any of the given primitives become
	// signalled, or when the timeout expires. The argument to the
	// callback is the primitive which became ready, or null if the
	// timeout expired.
	public static void WhenSignalled(Primitive[] prims,
					 Callback cb,
					 int timeoutMs = -1)
	{
	    Listener l = new Listener();

	    l.cont = (src) => {
		foreach (Primitive p in prims)
		    p.Unlisten();

		cb(src);
	    };

	    if (timeoutMs > 0)
		l.SetTimeout(timeoutMs);

	    foreach (Primitive p in prims)
		p.Listen(l);
	}

	public static void WhenSignalled(Primitive prim,
					 Callback cb,
					 int timeoutMs = -1)
	{
	    WhenSignalled(new Primitive[]{prim}, cb, timeoutMs);
	}
    }

    // The simplest of the  primitives is the event. It is
    // raised/cleared manually.
    class Event : Primitive
    {
	volatile bool state = false;

	public override bool Signalled
	{
	    get { return state; }
	}

	public void Raise()
	{
	    state = true;
	    fireListener();
	}

	public void Clear()
	{
	    state = false;
	}
    }

    // This primitive is the opposite of a semaphore. It can be used to
    // keep an waitable count of outstanding operations. It contains two
    // operations: Inc() and Dec(). The primitive becomes signalled
    // when the count is 0.
    class Counter : Primitive
    {
	object stateMutex = new object();
	int state = 0;

	public Counter() { }

	public Counter(int init)
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
		fireListener();
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
    class Channel<T> : Primitive
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

	    fireListener();
	}

	public void Close()
	{
	    lock (stateMutex)
		closeRequested = true;

	    fireListener();
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

    // This wrapper is a utility for waiting synchronously on 
    // primitives.
    class Sync
    {
	object mutex = new object();
	Primitive result;
	bool isReady = false;

	Sync() { }

	void Send(Primitive r)
	{
	    lock (mutex)
	    {
		isReady = true;
		result = r;
		Monitor.Pulse(mutex);
	    }
	}

	Primitive Recv()
	{
	    lock (mutex)
		while (!isReady)
		    Monitor.Wait(mutex);

	    return result;
	}

	public static Primitive Wait(Primitive[] prims,
				     int timeoutMs = -1)
	{
	    var s = new Sync();

	    Primitive.WhenSignalled(prims, s.Send, timeoutMs);
	    return s.Recv();
	}

	public static Primitive Wait(Primitive prim, int timeoutMs = -1)
	{
	    return Wait(new Primitive[]{prim}, timeoutMs);
	}
    }

    // This wrapper is a utility for scheduling continuations in the
    // thread pool.
    class Pool
    {
	public static void Continue(Primitive[] prims,
		Primitive.Callback cb, int timeoutMs = -1)
	{
	    Primitive.WhenSignalled(prims, (pr) =>
		ThreadPool.QueueUserWorkItem((obj) => cb(pr)),
		timeoutMs);
	}

	public static void Continue(Primitive prim,
		Primitive.Callback cb, int timeoutMs = -1)
	{
	    Primitive.WhenSignalled(prim, (pr) =>
		ThreadPool.QueueUserWorkItem((obj) => cb(pr)),
		timeoutMs);
	}
    }

    // This wrapper is a utility to produce Tasks for waiting for
    // primitives. It can either produce a blocking task, or
    // schedule an asynchronous continuation.
    class WaitTask
    {
	public static Task<Primitive> WaitAsync(Primitive[] prims,
						int timeoutMs = -1)
	{
	    var tsc = new TaskCompletionSource<Primitive>();

	    Primitive.WhenSignalled(prims,
		(src) => tsc.SetResult(src), timeoutMs);

	    return tsc.Task;
	}

	public static Task<Primitive> WaitAsync(Primitive prim,
						int timeoutMs = -1)
	{
	    return WaitAsync(new Primitive[]{prim}, timeoutMs);
	}
    }

    // This wrapper is another utility which produces a synchronous Gtk
    // event for a single  primitive. Its methods should be called
    // only from the Gtk thread.
    class GtkListener
    {
	public delegate void EventHandler(object sender,
		EventArgs args);

	public event EventHandler Signalled;
	public readonly Primitive Primitive;

	bool enabled = true;
	bool listening = false;

	public GtkListener(Primitive p)
	{
	    Primitive = p;
	    listen();
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
		Primitive.WhenSignalled(Primitive, (p) =>
		    Gtk.Application.Invoke(gtkHandler));
	}
    }
}
