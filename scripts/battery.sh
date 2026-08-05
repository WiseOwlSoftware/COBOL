#!/bin/bash
# THE COMPREHENSIVE BATTERY — run the independent legs CONCURRENTLY, read every verdict from its own file.
#
# ⛔ WHY THIS EXISTS. The comprehensive gate was being run leg-by-leg by hand: Conformance (~14 min), then Unit
# (~2 min), then characterization, then the guard, then the differential — strictly serially, on a 32-core
# machine, because plan §0's caution "run the long legs ONE AT A TIME" was being read as "never overlap
# anything". THE ACTUAL HAZARD IS NARROWER AND IS STATED IN §0: a leg that REBUILDS must not overlap a
# `--no-build` leg, because it swaps assemblies mid-run (that is how a Conformance run once produced no verdict
# at all). Two `--no-build` test assemblies cannot do that to each other.
#
# So the legs are grouped by what they WRITE, not by how long they take:
#   PHASE 0  build the solution ONCE          — every --no-build leg below depends on it (a stale test-bin
#                                               compiler DLL hides regressions: plan §0 "Mechanics")
#   PHASE 1  Conformance ∥ Unit ∥ Characterization — three independent --no-build assemblies, fully concurrent.
#            Wall-clock becomes the POLE (Conformance), not the SUM.
#   PHASE 2  guard-fast                       — REBUILDS the legacy CLI + legacy test projects, whose closure
#                                               includes Cobol.Net.Frontend. Must not overlap phase 1.
#   PHASE 3  GnuCOBOL differential            — drives cobol.exe; CPU-saturating, so it is not overlapped with
#                                               the guard (both would just split cores, and contention is the
#                                               known enemy of the guard's verdict).
#
# ⛔ EVERY VERDICT IS READ FROM A REDIRECTED FILE, NEVER `| tail -N` INTO A `&&` CHAIN (plan §0): `guard-fast`
# once reported exit 1 on a fully green run because the invoking chain ended in a `grep -c` that matched nothing.
#
# Usage:  bash scripts/battery.sh [outdir]        # everything
#         SKIP_GUARD=1 SKIP_DIFF=1 bash scripts/battery.sh    # the greenfield legs only
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"
OUT="${1:-${TMPDIR:-/tmp}/battery-$(date +%Y%m%d-%H%M%S)}"
mkdir -p "$OUT"
T0=$(date +%s); el() { echo "[+$(( $(date +%s) - T0 ))s] $*"; }
RC=0; SUMMARY="$OUT/summary.txt"; : > "$SUMMARY"
note() { echo "$1" | tee -a "$SUMMARY"; }

el "=== PHASE 0: build the solution (once) ==="
if ! dotnet build CobolSharp.sln -v quiet > "$OUT/build.log" 2>&1; then
    note "BUILD: FAILED — see $OUT/build.log"; tail -20 "$OUT/build.log"; exit 1
fi
note "BUILD: ok"

if [ "${SKIP_TESTS:-0}" != "1" ]; then
    el "=== PHASE 1: Conformance ∥ Unit ∥ Characterization (independent --no-build assemblies) ==="
    dotnet test tests/Cobol.Net.Tests.Conformance --no-build --verbosity quiet \
        --logger "trx;LogFileName=conformance.trx" --results-directory "$OUT" > "$OUT/conformance.log" 2>&1 &
    P_CONF=$!
    dotnet test tests/Cobol.Net.Tests.Unit --no-build --verbosity quiet \
        --logger "trx;LogFileName=unit.trx" --results-directory "$OUT" > "$OUT/unit.log" 2>&1 &
    P_UNIT=$!
    dotnet test tests/Cobol.Net.Tests.Characterization --no-build --verbosity quiet \
        > "$OUT/characterization.log" 2>&1 &
    P_CHAR=$!
    wait $P_CONF; RC_CONF=$?
    wait $P_UNIT; RC_UNIT=$?
    wait $P_CHAR; RC_CHAR=$?
    for leg in conformance unit characterization; do
        v=$(grep -E "^(Passed!|Failed!)" "$OUT/$leg.log" | tail -1)
        note "$(printf '%-16s %s' "$leg:" "${v:-NO VERDICT LINE — the leg produced no result, which is a FAILURE}")"
        [ -n "$v" ] || RC=1
    done
    [ "$RC_CONF" -eq 0 ] && [ "$RC_UNIT" -eq 0 ] && [ "$RC_CHAR" -eq 0 ] || RC=1
fi

if [ "${SKIP_GUARD:-0}" != "1" ]; then
    el "=== PHASE 2: guard-fast (rebuilds — never overlapped with phase 1) ==="
    bash scripts/guard-fast.sh > "$OUT/guard.log" 2>&1
    note "$(printf '%-16s %s' 'guard NIST:' "$(grep -E '^=== NIST: ' "$OUT/guard.log" | tail -1)")"
    note "$(printf '%-16s %s' 'guard audit:' "$(grep -E '^=== NIST AUDIT: (CLEAN|[0-9])' "$OUT/guard.log" | tail -1)")"
    note "$(printf '%-16s %s' 'guard verdict:' "$(grep -E '^=== (ALL GREEN|FAILURES)' "$OUT/guard.log" | tail -1)")"
    grep -q '^=== ALL GREEN ===' "$OUT/guard.log" || RC=1
fi

if [ "${SKIP_DIFF:-0}" != "1" ]; then
    el "=== PHASE 3: GnuCOBOL external differential ==="
    python3 scripts/gnucobol_differential.py --exe src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe \
        --report "$OUT/gnucobol-report.json" > "$OUT/gnucobol.log" 2>&1
    note "$(printf '%-16s %s' 'gnucobol:' "$(grep -E '^cases run:' "$OUT/gnucobol.log" | tail -1)")"
    grep -E '^  (AGREE|WE_)' "$OUT/gnucobol.log" | sed 's/^/                 /' | tee -a "$SUMMARY"
    if grep -q 'NO COMPILER VERDICT' "$OUT/gnucobol.log"; then
        note "gnucobol:        ⛔ cases with NO COMPILER VERDICT — see $OUT/gnucobol.log"; RC=1
    fi
    # ⛔ THE PER-CASE DIFF IS THE LEG THAT MATTERS, NOT THE FOUR TOTALS ABOVE. Identical totals are consistent
    # with OFFSETTING flips, so this gates on the per-case verdict line (never the exit code — §0's standing
    # rule) and names every flip in the log. An ABSENT baseline is a RED, not a pass: without it the run
    # cannot claim zero flips, which is precisely the hole this closes.
    note "$(printf '%-16s %s' 'gnucobol diff:' "$(grep -E '^=== DIFFERENTIAL: ' "$OUT/gnucobol.log" | tail -1)")"
    grep -E '^(  corpus changed:|    (FLIP|NEW|REMOVED) )' "$OUT/gnucobol.log" \
        | sed 's/^/                 /' | tee -a "$SUMMARY"
    if ! grep -qF '=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===' "$OUT/gnucobol.log"; then
        note "gnucobol diff:   ⛔ per-case verdict flips (or no baseline) — see $OUT/gnucobol.log"; RC=1
    fi
fi

el "=== BATTERY SUMMARY (artifacts in $OUT) ==="
cat "$SUMMARY"
[ "$RC" -eq 0 ] && echo "=== BATTERY: ALL GREEN ===" || echo "=== BATTERY: NOT GREEN (rc=$RC) ==="
exit $RC
