#!/bin/bash
# Run ONE NIST group serially in an isolated scratch directory, emitting one "TEST: VERDICT" line per test to
# stdout. Used by scripts/guard-fast.sh to parallelize the NIST suite across cores with airtight isolation.
#
#   $1 = repo root (absolute)
#   $2 = group id (informational)
#   $3 = ordered, space-separated test names that must share a dir + run order (a producer/consumer chain, or a
#        single self-contained test)
#
# Mirrors scripts/guard.sh's per-test run+compare logic EXACTLY (same normalize, same outfile/print-file/stdout
# resolution, same FAIL*/footer rules) so a verdict here is identical to the serial guard's — only the working
# directory differs. The dir starts clean (mktemp) and is NOT cleaned between tests within the group, so a chain's
# shared TF### data files accumulate in declaration order exactly as they do in the serial guard's shared dir.
set -u

ROOT="$1"; GROUP="$2"; TESTS="$3"
OUT="$ROOT/tests/nist/output"
RUNTIME="$ROOT/src/CobolSharp.Runtime/bin/Debug/net10.0/CobolSharp.Runtime.dll"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cp "$RUNTIME" "$WORK/"
export COBOL_SWITCH_1=ON

# tr -d '\r' FIRST (CRLF golden vs LF program output on Linux); must precede 's/ *$//' which is a no-op while a
# trailing \r remains. Idempotent on Windows. See the matching note in scripts/guard.sh.
normalize() { tr -d '\r' < "$1" 2>/dev/null | sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/'; }

for test in $TESTS; do
    dll="$OUT/$test.dll"
    if [ ! -f "$dll" ]; then
        echo "$test: COMPILE FAILED — REGRESSION!"
        continue
    fi
    cp "$dll" "$WORK/"
    [ -f "$OUT/$test.runtimeconfig.json" ] && cp "$OUT/$test.runtimeconfig.json" "$WORK/"

    outfile="$(echo "$test" | tr '[:upper:]' '[:lower:]').txt"
    stdoutfile="$WORK/${test}-stdout.txt"
    datafile="$ROOT/tests/nist/data/$test.dat"
    if [ -f "$datafile" ]; then
        (cd "$WORK" && dotnet "$test.dll" 2>/dev/null || true) < "$datafile" > "$stdoutfile"
    else
        (cd "$WORK" && dotnet "$test.dll" 2>/dev/null || true) > "$stdoutfile"
    fi

    validfile="$ROOT/tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        fc=$(grep -c "FAIL\*" "$WORK/$outfile" 2>/dev/null || true); fc=${fc:-0}
        echo "$test: NO BASELINE (${fc} FAIL* — pending fix)"
        continue
    fi

    actual=""
    if   diff <(normalize "$validfile") <(normalize "$WORK/$outfile")        >/dev/null 2>&1; then actual="$WORK/$outfile"
    elif diff <(normalize "$validfile") <(normalize "$WORK/print-file.txt")  >/dev/null 2>&1; then actual="$WORK/print-file.txt"
    elif diff <(normalize "$validfile") <(normalize "$stdoutfile")           >/dev/null 2>&1; then actual="$stdoutfile"
    fi

    if [ -z "$actual" ]; then
        echo "$test: DIFF — REGRESSION!"
        continue
    fi

    fc=$(grep -c "FAIL\*" "$actual" 2>/dev/null || true); fc=${fc:-0}
    ff=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$actual" 2>/dev/null | grep -oE "^[0-9]+" | head -1); ff=${ff:-0}
    if [ "$ff" -gt 0 ] 2>/dev/null; then
        echo "$test: FOOTER ${ff} TEST(S) FAILED — REGRESSION!"
    elif [ "$fc" -gt 0 ] 2>/dev/null; then
        echo "$test: MATCH (${fc} FAIL*)"
    else
        echo "$test: MATCH"
    fi
done
