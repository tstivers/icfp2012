find ../maps/*.map -exec mono LambdaLifter.Cli.exe {} \; > log.txt
grep MapState log.txt
echo Won: `grep Won log.txt | wc -l`  Lost: `grep MapState log.txt | grep -v Won | wc -l`  Score: `grep Score log.txt | awk '{ sum += $2 } END { print sum }'`
