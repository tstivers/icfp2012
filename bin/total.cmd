@runtests | awk "{ sum += $3; print $0; } END { print \"\"; print \"                Total Score: \" sum }"