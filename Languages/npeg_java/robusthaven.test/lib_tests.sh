#!/bin/bash 

JUNIT_PATH=/usr/share/java

fn_test_status()
{
    case $1 in
	0)
	    echo -n "Passed"
	    ;;
	*)
	    echo -n "Failed"
	    echo
	    ;;
    esac
}


fn_test_run()
{
    pattern=$1
    isverbose=$2

    for TEST in ${pattern}; do
	EXT=${TEST#*.}
	BASENAME=${TEST%.*}
	SUBCLASS=${BASENAME#*\$}
	DIRNAME=${TEST%/*}

	if [ "${SUBCLASS}" != "${BASENAME}" ]; then
	    continue
	fi

	if [ "x${EXT}" == "xclass" ]; then
	    if [ "x${DIRNAME}" == "xparser_tests" ]; then
		CLASSFILE=${TEST#*/}
		BASENAME=${CLASSFILE%.*}
		java -ea -cp ../robusthaven.jar:${JUNIT_PATH}/junit.jar:testparsers.jar:parser_tests.jar \
		    parser_tests.${BASENAME} >out.teststdin 2>out.teststderr
		status=$?
	    else 
		java -ea -cp ../robusthaven.jar:${JUNIT_PATH}/junit.jar:. ${BASENAME} \
		    >out.teststdin 2>out.teststderr
		status=$?
	    fi
	    
	    printf "%-50.43s  %-22s\n" $TEST `fn_test_status $status`

	    if [ $status -ne 0 ] || [ $isverbose = 1 ]; then
		cat out.teststdin
		cat out.teststderr
		echo
	    fi
	fi
    done
}


fn_test_memory()
{
    pattern=$1
    isverbose=$2
    for TEST in ${pattern}*; do
	if [ -x ${TEST} ]; then
			#-v
	    valgrind --tool=memcheck --show-reachable=yes --leak-check=yes ./${TEST} >out.teststdin 2>out.teststderr
	    cat out.teststderr
	fi
    done
}
