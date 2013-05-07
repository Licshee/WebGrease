@echo off
SETLOCAL enableextensions enabledelayedexpansion

set TF="%ProgramFiles(x86)%\Microsoft Visual Studio 11.0\Common7\IDE\tf.exe"

if exist %TF% (
	for /F %%I in (%1) do (
		set ATTRIBS=%%~aI
		set READ_ATTRIB=!ATTRIBS:~1,1!
		if !READ_ATTRIB!==- (
			echo %%I is already checked out.
		) else (
			%TF% checkout %%I
		)
	)
) else (
  ECHO Could not find tf.exe!
  EXIT 1
)