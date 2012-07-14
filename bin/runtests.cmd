@echo off
@for /f %%G in ('"gfind ../maps/*.map"') do (	
	for /f %%a in ('..\src\LambdaLifter.Cli\bin\Debug\LambdaLifter.Cli.exe %%G ^| grep MapState ^| awk ^"{print $2}^"') do (	
		echo %%G %%a
	)	
)
@gfind ../maps/*.map -exec ..\src\LambdaLifter.Cli\bin\Debug\LambdaLifter.Cli.exe {} ;  | grep Score | awk "{ sum += $2 } END { print sum }"