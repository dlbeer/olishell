CSC ?= dmcs
BINARY = olishell.exe
SOURCE = \
    App.cs \
    ConsoleLog.cs \
    ITC.cs \
    DebugView.cs \
    Debugger.cs \
    SampleQueue.cs \
    PowerView.cs \
    DebugManager.cs \
    AppMenu.cs \
    Settings.cs \
    PreferencesDialog.cs \
    DebugPane.cs

olishell.exe: $(SOURCE)
	$(CSC) -target:winexe -pkg:gtk-sharp-2.0 -out:$@ $(SOURCE)

clean:
	rm -f olishell.exe
