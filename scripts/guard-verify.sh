#!/bin/bash
# Prove scripts/guard-fast.sh is equivalent to the serial scripts/guard.sh: run both, extract the per-test
# "NAME: VERDICT" lines, and diff them. They MUST be byte-identical. This is the gate that lets us trust the
# parallel guard — run it after any change to the test corpus, the grouping, or guard-fast.sh itself.
# Exit 0 iff the verdict lists are identical AND both runs were ALL GREEN.
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"
TMP="${TMPDIR:-/tmp}"

echo "=== serial scripts/guard.sh ==="
bash scripts/guard.sh > "$TMP/gv_serial.log" 2>&1; SER=$?
echo "=== parallel scripts/guard-fast.sh ==="
bash scripts/guard-fast.sh > "$TMP/gv_fast.log" 2>&1; FAST=$?

# Normalize verdict lines (strip indent, keep the FAIL* count) and sort so order (which differs by design) is moot.
verdicts() {
    grep -E "^ *[A-Z][A-Z0-9]+: (MATCH|DIFF|FOOTER|COMPILE FAILED|NO BASELINE)" "$1" \
        | sed 's/^ *//' | sort
}
verdicts "$TMP/gv_serial.log" > "$TMP/gv_serial.verdicts"
verdicts "$TMP/gv_fast.log"   > "$TMP/gv_fast.verdicts"

echo
if diff "$TMP/gv_serial.verdicts" "$TMP/gv_fast.verdicts" > "$TMP/gv_diff.txt"; then
    echo "=== VERDICTS IDENTICAL ($(wc -l < "$TMP/gv_serial.verdicts" | tr -d ' ') tests) — guard-fast PROVEN equivalent to guard.sh ==="
    RC=0
else
    echo "=== VERDICT MISMATCH (< serial / > fast) — guard-fast is NOT yet equivalent; fix grouping ==="
    cat "$TMP/gv_diff.txt"
    RC=1
fi
echo "serial guard rc=$SER   fast guard rc=$FAST"
[ "$SER" -eq 0 ] && [ "$FAST" -eq 0 ] || RC=1
exit $RC
