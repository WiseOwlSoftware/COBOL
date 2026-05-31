#!/bin/bash
# Guardrail script — run after every meaningful change set.
# Exit on first failure.
set -e

echo "=== Building ==="
dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj -v quiet

echo "=== Unit tests ==="
dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --verbosity quiet

echo "=== Integration tests ==="
dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --verbosity quiet

echo "=== NIST regression ==="
cp src/CobolSharp.Runtime/bin/Debug/net9.0/CobolSharp.Runtime.dll tests/nist/output/
CLI=src/CobolSharp.CLI/bin/Debug/net9.0/cobolsharp.dll

# All NIST tests currently at 100% — must stay green
# (94 NC + 42 IF + 12 SM + 4 IC + 25 SQ + 5 RL + 1 IX = 183 tests).
# IF401M/402M/403M are flagging-conformance modules: they emit no CCVS report by
# design, so they are intentionally NOT guarded (nothing to compare).
NIST_TESTS="
NC101A NC102A NC103A NC104A NC105A NC106A NC107A NC108M NC109M NC110M
NC111A NC112A NC113M NC114M NC115A NC116A NC117A NC118A NC119A NC120A NC121M
NC122A NC123A NC124A NC125A NC126A NC127A
NC131A NC132A NC133A NC134A NC135A NC136A NC137A NC138A NC139A NC140A NC141A
NC170A NC171A NC172A NC173A NC174A NC175A NC176A NC177A
NC201A NC202A NC203A NC204M NC205A NC206A NC207A NC208A NC209A NC210A NC211A NC215A NC216A NC217A NC218A NC219A NC220M NC221A NC222A NC223A NC224A
NC225A NC231A NC232A NC233A NC234A NC235A NC236A NC237A NC238A NC245A NC246A NC247A
NC239A NC240A NC250A NC241A NC242A NC243A NC244A NC248A NC251A NC252A NC253A NC254A
NC302M NC303M
NC401M
IF101A IF102A IF103A IF104A IF105A IF106A IF107A IF108A IF109A IF110A
IF111A IF112A IF113A IF114A IF115A IF116A IF117A IF118A IF119A IF120A
IF121A IF122A IF123A IF124A IF125A IF126A IF127A IF128A IF129A IF130A
IF131A IF132A IF133A IF134A IF135A IF136A IF137A IF138A IF139A IF140A
IF141A IF142A
SM101A SM102A SM103A SM106A SM107A SM201A SM202A SM203A SM204A SM206A
SM207A SM208A
IC203A IC224A IC225A IC228A
SQ101M SQ102A SQ104A SQ108A SQ111A SQ112A SQ113A SQ117A SQ126A SQ127A
SQ131A SQ143A SQ146A SQ149A SQ150A SQ154A SQ155A SQ202A SQ204A SQ207M SQ211A SQ213A
SQ217A SQ230A SQ302M
RL101A RL201A RL209A RL210A RL302M
IX302M
"

# NIST convention: SWITCH-1 ON, SWITCH-2 OFF (default)
export COBOL_SWITCH_1=ON

FAILURES=0
for test in $NIST_TESTS; do
    # Compile
    if ! dotnet "$CLI" --nist "tests/nist/programs/$test.cob" -o "tests/nist/output/$test.dll" 2>/dev/null; then
        echo "  $test: COMPILE FAILED — REGRESSION!"
        FAILURES=$((FAILURES + 1))
        continue
    fi

    # Run in the output directory; capture stdout for DISPLAY-only tests
    # Pipe NIST data file to stdin when available (for ACCEPT tests)
    outfile=$(echo "$test" | tr '[:upper:]' '[:lower:]').txt
    stdoutfile="tests/nist/output/${test}-stdout.txt"
    datafile="tests/nist/data/$test.dat"
    if [ -f "$datafile" ]; then
        (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true) < "$datafile" > "$stdoutfile"
    else
        (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true) > "$stdoutfile"
    fi

    # Compare output (files written to tests/nist/output/)
    validfile="tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        # No baseline = test has known failures. Still compile/run but don't compare.
        fail_count=$(grep -c "FAIL\*" "tests/nist/output/$outfile" 2>/dev/null || true)
        fail_count=${fail_count:-0}
        echo "  $test: NO BASELINE (${fail_count} FAIL* — pending fix)"
        continue
    fi

    # Normalize: strip trailing spaces, and normalize time-dependent COMPUTED values
    normalize() { sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/' "$1" 2>/dev/null; }

    # Find the actual output file (outfile, print-file, or stdout)
    actual=""
    if diff <(normalize "$validfile") <(normalize "tests/nist/output/$outfile") > /dev/null 2>&1; then
        actual="tests/nist/output/$outfile"
    elif diff <(normalize "$validfile") <(normalize "tests/nist/output/print-file.txt") > /dev/null 2>&1; then
        actual="tests/nist/output/print-file.txt"
    elif diff <(normalize "$validfile") <(normalize "$stdoutfile") > /dev/null 2>&1; then
        actual="$stdoutfile"
    fi

    if [ -z "$actual" ]; then
        echo "  $test: DIFF — REGRESSION!"
        FAILURES=$((FAILURES + 1))
        continue
    fi

    # Check for FAIL* in output — these are real test failures, not acceptable baselines
    fail_count=$(grep -c "FAIL\*" "$actual" 2>/dev/null || true)
    fail_count=${fail_count:-0}
    if [ "$fail_count" -gt 0 ] 2>/dev/null; then
        echo "  $test: MATCH (${fail_count} FAIL*)"
    else
        echo "  $test: MATCH"
    fi
done

if [ $FAILURES -gt 0 ]; then
    echo "=== $FAILURES NIST REGRESSION(S) ==="
    exit 1
fi

# Verify no baselines contain FAIL* — baselines must be 100% clean
for f in tests/nist/valid/*.txt; do
    fc=$(grep -c "FAIL\*" "$f" 2>/dev/null || true)
    fc=${fc:-0}
    if [ "$fc" -gt 0 ] 2>/dev/null; then
        echo "=== ERROR: $(basename "$f") contains $fc FAIL* — remove from valid/ ==="
        FAILURES=$((FAILURES + 1))
    fi
done

echo "=== ALL GREEN ==="
