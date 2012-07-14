@ECHO OFF
..\..\gnuwin32\bin\find ..\maps -name *.map -exec ..\src\LambdaLifter.Cli\bin\Debug\LambdaLifter.Cli.exe {} ; | ..\..\gnuwin32\bin\grep MapState;