@ECHO OFF
SETLOCAL
"%~dp0tools\NuGet.exe" install IronPython.Interpreter -Version 2.7.4 -OutputDirectory "%~dp0tools" -NonInteractive -Verbosity quiet -ExcludeVersion
if "%PROCESSOR_ARCHITECTURE%"=="x86" GOTO 32BIT
	SET IRONPYTHON_EXE=ipy64.exe
	GOTO END
:32BIT
	SET IRONPYTHON_EXE=ipy.exe
:END
"%~dp0tools\IronPython.Interpreter\tools\%IRONPYTHON_EXE%" -tt "%~dp0tools\make.py" "%~dp0makefile.py" %*
