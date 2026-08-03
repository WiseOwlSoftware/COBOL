#!/bin/bash
# Prove scripts/guard-fast.sh is equivalent to the serial scripts/guard.sh: run both, extract the per-test
# "NAME: VERDICT" lines, and diff them. They MUST be byte-identical.
# Exit 0 iff the verdict lists are identical AND both runs were ALL GREEN.
#
# ⚠ THIS IS NO LONGER THE PRIMARY CHECK, AND THE HEADER USED TO SAY IT WAS. Both guards now run
# scripts/guard-nist-audit.sh, which compares every program's verdict against tests/nist/corpus.tsv — an
# ABSOLUTE check against a committed manifest. This script is RELATIVE: it compares the two guards to each
# other, so it cannot see them deviating together, and it is necessarily empty whenever both are audit-clean.
# The old instruction "refine groupings only with the diff green" is superseded by "refine groupings only with
# the AUDIT clean", which is cheaper (one guard run, not two) and strictly stronger. Keep this script for the
# case the audit cannot cover: a difference in the two guards' own MECHANICS that the manifest is blind to
# (working-directory isolation, run ORDER within a group, the compile path).
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"
TMP="${TMPDIR:-/tmp}"

echo "=== serial scripts/guard.sh ==="
bash scripts/guard.sh > "$TMP/gv_serial.log" 2>&1; SER=$?
echo "=== parallel scripts/guard-fast.sh ==="
bash scripts/guard-fast.sh > "$TMP/gv_fast.log" 2>&1; FAST=$?

# Normalize verdict lines (strip indent, keep the FAIL* count) and sort so order (which differs by design) is moot.
# ⛔ THE VOCABULARY MUST BE COMPLETE. The previous pattern omitted `LEGACY DIVERGENT`, so 11 programs were
# silently dropped from BOTH sides and the equivalence proof never compared them — the same defect class this
# whole wave closes (a filter that quietly excludes reads as agreement). Any line that looks like a verdict but
# matches no known word is surfaced by the UNKNOWN check below rather than discarded.
VERDICT_WORDS="MATCH|DIFF|FOOTER|COMPILE FAILED|COMPILE NO-VERDICT|RUN NO-VERDICT|NO BASELINE|LEGACY DIVERGENT"
verdicts() {
    grep -E "^ *[A-Z][A-Z0-9]+: ($VERDICT_WORDS)" "$1" | sed 's/^ *//' | sort
}
# Every "NAME: something" line the run emitted, so a NEW verdict word cannot slip past the filter above.
all_verdict_shaped() { grep -E "^ *[A-Z][A-Z0-9]+: " "$1" | sed 's/^ *//' | sort; }
verdicts "$TMP/gv_serial.log" > "$TMP/gv_serial.verdicts"
verdicts "$TMP/gv_fast.log"   > "$TMP/gv_fast.verdicts"

# A verdict-shaped line the filter did not recognize would otherwise vanish from the comparison.
RC_UNKNOWN=0
for side in serial fast; do
    all_verdict_shaped "$TMP/gv_$side.log" > "$TMP/gv_$side.all"
    if ! diff -q "$TMP/gv_$side.all" "$TMP/gv_$side.verdicts" > /dev/null; then
        echo "=== $side: verdict-shaped lines the filter does NOT recognize (teach VERDICT_WORDS) ==="
        comm -23 "$TMP/gv_$side.all" "$TMP/gv_$side.verdicts"
        RC_UNKNOWN=1
    fi
done

echo
if diff "$TMP/gv_serial.verdicts" "$TMP/gv_fast.verdicts" > "$TMP/gv_diff.txt"; then
    echo "=== VERDICTS IDENTICAL ($(wc -l < "$TMP/gv_serial.verdicts" | tr -d ' ') tests) — guard-fast PROVEN equivalent to guard.sh ==="
    RC=0
else
    echo "=== VERDICT MISMATCH (< serial / > fast) — guard-fast is NOT yet equivalent; fix grouping ==="
    cat "$TMP/gv_diff.txt"
    RC=1
fi
echo "serial guard rc=$SER   fast guard rc=$FAST   unrecognized-verdict-words=$RC_UNKNOWN"
[ "$SER" -eq 0 ] && [ "$FAST" -eq 0 ] && [ "$RC_UNKNOWN" -eq 0 ] || RC=1
exit $RC
