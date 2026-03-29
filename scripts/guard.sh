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

# All NIST tests currently at 100% — must stay green (86 tests)
NIST_TESTS="
NC101A NC102A NC103A NC104A NC105A NC106A NC107A NC108M NC109M NC110M
NC111A NC112A NC113M NC114M NC115A NC116A NC117A NC118A NC119A NC120A NC121M
NC122A NC123A NC124A NC126A NC127A
NC131A NC132A NC133A NC134A NC135A NC136A NC137A NC138A NC139A NC140A NC141A
NC170A NC171A NC172A NC173A NC174A NC175A NC176A NC177A
NC202A NC203A NC204M NC206A NC207A NC208A NC209A NC210A NC211A NC214M NC215A NC216A NC217A NC218A NC219A NC221A NC222A NC223A NC224A
NC231A NC232A NC233A NC234A NC235A NC236A NC238A NC245A NC246A NC247A
NC239A NC240A NC241A NC242A NC243A NC244A NC248A NC251A NC252A NC253A NC254A
NC302M
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
    outfile=$(echo "$test" | tr '[:upper:]' '[:lower:]').txt
    stdoutfile="tests/nist/output/${test}-stdout.txt"
    (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true) > "$stdoutfile"

    # Compare output (files written to tests/nist/output/)
    validfile="tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        echo "  $test: NO VALID FILE — skipping"
        continue
    fi

    # Normalize: strip trailing spaces, and normalize time-dependent COMPUTED values
    normalize() { sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/' "$1" 2>/dev/null; }

    if diff <(normalize "$validfile") <(normalize "tests/nist/output/$outfile") > /dev/null 2>&1; then
        echo "  $test: MATCH"
    elif diff <(normalize "$validfile") <(normalize "tests/nist/output/print-file.txt") > /dev/null 2>&1; then
        echo "  $test: MATCH"
    elif diff <(normalize "$validfile") <(normalize "$stdoutfile") > /dev/null 2>&1; then
        echo "  $test: MATCH"
    else
        echo "  $test: DIFF — REGRESSION!"
        FAILURES=$((FAILURES + 1))
    fi
done

if [ $FAILURES -gt 0 ]; then
    echo "=== $FAILURES NIST REGRESSION(S) ==="
    exit 1
fi

echo "=== ALL GREEN ==="
