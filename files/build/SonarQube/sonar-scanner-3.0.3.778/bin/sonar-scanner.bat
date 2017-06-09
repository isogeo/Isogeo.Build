@ECHO OFF
:: Trick to use the scanner that is actually installed...
IF "%_SONAR_SCANNER_HOME%"=="" (
    ECHO _SONAR_SCANNER_HOME is not set
    EXIT /B 255
)
IF NOT EXISTS "%_SONAR_SCANNER_HOME%\bin\sonar-scanner.bat" (
    ECHO _SONAR_SCANNER_HOME is invalid: %_SONAR_SCANNER_HOME%
    EXIT /B 255
)

CALL "%_SONAR_SCANNER_HOME%\bin\sonar-scanner.bat"