find ../maps/*.map -exec mono LambdaLifter.Cli.exe {} \; > log.txt
cat log.txt
echo
echo Won: `grep Won log.txt | wc -l`  Lost: `grep -v Won log.txt | wc -l`  Score: `cat log.txt | awk '{ sum += $3 } END { print sum }'`
