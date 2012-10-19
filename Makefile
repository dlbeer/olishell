CSC ?= dmcs
BINARY = olishell.exe
SOURCE = \
    App.cs \
    ConsoleLog.cs \
    ITC.cs \
    DebugView.cs \
    Debugger.cs \
    SampleQueue.cs \
    PowerView.cs

olishell.exe: $(SOURCE)
	$(CSC) -pkg:gtk-sharp-2.0 -out:$@ $(SOURCE)

clean:
	rm -f olishell.exe
