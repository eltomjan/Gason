setlocal enabledelayedexpansion enableextensions
set LIBS=
for %%x in (*.dll) do (
  echo %%x|findstr ",">nul 2>&1
  if errorlevel 1 (set LIBS=!LIBS!,%%x) else (set LIBS=!LIBS!,"%%x")
)
if "%LIBS%"=="" (for /F %%v in ('dir /s /OD %WINDIR%\Microsoft.NET\csc.exe /B') do echo %%v %%* > cscLatest.bat ) else (for /F %%v in ('dir /s /OD %WINDIR%\Microsoft.NET\csc.exe /B') do echo %%v /r:%LIBS:~1% %%* > cscLatest.bat )
rem if "%1" == "" cscLatest.bat *.cs
rem cscLatest.bat %*
