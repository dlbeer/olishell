CSC ?= gmcs
BINARY = olishell.exe
SOURCE = \
    src/App.cs \
    src/ConsoleLog.cs \
    src/ITC.cs \
    src/DebugView.cs \
    src/Debugger.cs \
    src/SampleQueue.cs \
    src/PowerView.cs \
    src/DebugManager.cs \
    src/AppMenu.cs \
    src/Settings.cs \
    src/PreferencesDialog.cs \
    src/DebugPane.cs \
    src/AppToolbar.cs

olishell.exe: $(SOURCE)
	$(CSC) -pkg:gtk-sharp-2.0 -out:$@ $(SOURCE)

clean:
	rm -f olishell.exe
