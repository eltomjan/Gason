call cscLatest.bat *.cs -define:DoubleLinked -debug+
Program
pause
call cscLatest.bat *.cs -define:KEY_SPLIT,DoubleLinked -debug+
Program
pause
del Program.exe
del Program.pdb
