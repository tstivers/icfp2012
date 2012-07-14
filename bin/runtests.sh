find ../maps -name *.map -exec echo {} \; -exec mono client.exe {} \; > log.txt
grep MapState log.txt
echo Won: `grep Won log.txt | wc -l`
echo Lost: `grep MapState log.txt | grep -v Won | wc -l`
