call cscLatest.bat *.cs -debug+
Program
pause
call cscLatest.bat *.cs -define:KEY_SPLIT -debug+
Program
pause
del Program.exe
del Program.pdb
