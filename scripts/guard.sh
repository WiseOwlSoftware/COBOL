#!/bin/bash
# Phase B guardrail script — run after every meaningful change set.
# Exit on first failure.
set -e

echo "=== Building ==="
dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj -v quiet

echo "=== Unit tests (99 expected) ==="
dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --verbosity quiet

echo "=== Integration tests (30 pass, 7 skip expected) ==="
dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --verbosity quiet

echo "=== NIST regression ==="
cp src/CobolSharp.Runtime/bin/Debug/net8.0/CobolSharp.Runtime.dll tests/nist/output/
for test in NC101A NC171A NC106A NC176A NC116A NC118A; do
    dotnet run --project src/CobolSharp.CLI -- tests/nist/programs/$test.cob -o tests/nist/output/$test.dll 2>/dev/null
    dotnet tests/nist/output/$test.dll 2>/dev/null
    if diff <(sed 's/ *$//' tests/nist/valid/$test.txt) <(sed 's/ *$//' print-file.txt) > /dev/null 2>&1; then
        echo "  $test: MATCH"
    else
        echo "  $test: DIFF — REGRESSION!"
        exit 1
    fi
done

echo "=== ALL GREEN ==="
