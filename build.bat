@ECHO OFF
::--------------------------------------------------------------------
:: Usage: "build [/clean | /rebuild | /release] [/log] [/NoPause] [/?]"
::
::                 /clean    - Cleans the project
::                 /rebuild  - Cleans and builds the project (default)
::                 /release  - Rebuilds the project and performs additional operations
::
::                 /log      - Creates a detailed log
::
::                 /NoPause  - Does not pause after completion
::                 /?        - Gets the usage for this script
::--------------------------------------------------------------------



:: Reset ERRORLEVEL
VERIFY OTHER 2>nul
SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION
IF ERRORLEVEL 1 GOTO ERROR_EXT

SET NO_PAUSE=0
SET PROJECT=Isogeo.Build.proj
SET TARGET=Rebuild
SET GENERATE_DOCUMENTATION=False
SET VERBOSITY=minimal
GOTO ARGS



:: -------------------------------------------------------------------
:: Builds the project
:: -------------------------------------------------------------------
:BUILD
.nuget\NuGet.exe install ".nuget\packages.config" -o packages -source "https://nuget.org/api/v2/" -source "%LocalAppData%\NuGet\Cache"
msbuild.exe %PROJECT% /nologo /t:%TARGET% /m:%NUMBER_OF_PROCESSORS% /p:GenerateDocumentation="%GENERATE_DOCUMENTATION%" /fl /flp:logfile=build.log;verbosity=%VERBOSITY%;encoding=UTF-8 /nr:False

IF ERRORLEVEL 1 (
    COLOR 4E
)
GOTO END



:: -------------------------------------------------------------------
:: Parse command line argument values
:: Note: Currently, last one on the command line wins (ex: /rebuild /clean == /clean)
:: -------------------------------------------------------------------
:ARGS
::IF NOT "x%~4"=="x" GOTO ERROR_USAGE

:ARGS_PARSE
IF /I "%~1"=="/clean"      SET TARGET=Clean& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/rebuild"    SET TARGET=Rebuild& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/release"    SET TARGET=Release& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/doc"        SET GENERATE_DOCUMENTATION=True& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/log"        SET VERBOSITY=diagnostic& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/NoPause"    SET NO_PAUSE=1& SHIFT & GOTO ARGS_PARSE
IF /I "%~1"=="/?"          GOTO ERROR_USAGE
IF    "%~1" EQU ""         GOTO ARGS_DONE
ECHO Unknown command-line switch: %~1
GOTO ERROR_USAGE

:ARGS_DONE
GOTO SETENV



:: -------------------------------------------------------------------
:: Set environment variables
:: -------------------------------------------------------------------
:SETENV
CALL :SetMSBuildToolsPathHelper > nul 2>&1
IF ERRORLEVEL 1 GOTO ERROR_MSBUILD

SET PATH=%CD%\misc\;%MSBuildToolsPath%;%PATH%
GOTO BUILD

:SetMSBuildToolsPathHelper
SET MSBuildToolsPath=
FOR /F "tokens=1,2*" %%i in ('REG QUERY HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0 /V MSBuildToolsPath') DO (
    IF "%%i"=="MSBuildToolsPath" (
        SET "MSBuildToolsPath=%%k"
    )
)
IF "%MSBuildToolsPath%"=="" EXIT /B 1
EXIT /B 0



:: -------------------------------------------------------------------
:: Errors
:: -------------------------------------------------------------------
:ERROR_EXT
ECHO Could not activate command extensions
GOTO END

:ERROR_MSBUILD
ECHO Could not find MSBuild
GOTO END

:ERROR_USAGE
ECHO Usage: "build [/clean | /rebuild | /release] [/log] [/NoPause] [/?]"
ECHO.
ECHO                 /clean    - Cleans the project
ECHO                 /rebuild  - Cleans and builds the project (default)
ECHO                 /release  - Rebuilds the project and performs additional operations
ECHO.
ECHO                 /log      - Creates a detailed log
ECHO.
ECHO                 /NoPause  - Does not pause after completion
ECHO                 /?        - Gets the usage for this script
GOTO END



:: -------------------------------------------------------------------
:: End
:: -------------------------------------------------------------------
:END
@IF NOT "%NO_PAUSE%"=="1" PAUSE
ENDLOCAL
