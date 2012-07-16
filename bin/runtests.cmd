@echo off
for /f %%G in ('"gfind ../maps/*.map"') do (	
		..\src\LambdaLifter.Cli\bin\Debug\LambdaLifter.Cli.exe %%G 
)
REM gfind ../maps/*.map -exec ..\src\LambdaLifter.Cli\bin\Debug\LambdaLifter.Cli.exe {} ;  | grep ^^Score | awk "{ sum += $2 } END { print sum }"