#!/bin/bash
# Guardrail script — run after every meaningful change set.
# Exit on first failure.
set -e

echo "=== Building ==="
dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj -v quiet

echo "=== Unit tests ==="
dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --verbosity quiet

echo "=== Integration tests (153 pass, 1 skip expected) ==="
dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --verbosity quiet

echo "=== NIST regression ==="
cp src/CobolSharp.Runtime/bin/Debug/net9.0/CobolSharp.Runtime.dll tests/nist/output/
CLI=src/CobolSharp.CLI/bin/Debug/net9.0/cobolsharp.dll

# All NIST tests currently at 100% — must stay green (30 tests)
NIST_TESTS="
NC101A NC102A NC103A NC107A
NC111A NC112A NC115A NC117A
NC122A NC123A NC124A NC126A NC127A
NC131A NC132A NC136A NC137A NC140A NC141A
NC176A
NC202A NC206A NC207A NC210A
NC221A NC233A
NC211A
NC239A NC240A NC241A NC248A NC253A NC254A
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

    # Run in the output directory to keep generated files out of project root
    outfile=$(echo "$test" | tr '[:upper:]' '[:lower:]').txt
    (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true)

    # Compare output (files written to tests/nist/output/)
    validfile="tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        echo "  $test: NO VALID FILE — skipping"
        continue
    fi

    if diff <(sed 's/ *$//' "$validfile") <(sed 's/ *$//' "tests/nist/output/$outfile") > /dev/null 2>&1; then
        echo "  $test: MATCH"
    elif diff <(sed 's/ *$//' "$validfile") <(sed 's/ *$//' "tests/nist/output/print-file.txt") > /dev/null 2>&1; then
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
