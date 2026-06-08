#!/bin/bash
# Parallel guard — the same checks as scripts/guard.sh, but the 364-program NIST suite runs across cores with
# per-group isolation. Intended to be PROVEN equivalent to the serial guard via scripts/guard-verify.sh (which
# diffs the per-test verdicts). Until that diff is clean, trust scripts/guard.sh as the authority.
#
# WHY THIS IS SAFE (isolation model): NIST tests couple ONLY through shared on-disk data files (TF### producer/
# consumer chains), which are confined WITHIN a suite. So:
#   - self-contained suites (NC/IF/RW): every test runs alone in its own scratch dir  -> fully parallel.
#   - file-I/O suites (IC/SQ/RL/IX/ST/SM/OBSQ): the whole suite runs SERIALLY (guard.sh order) in one scratch dir
#     -> identical to serial behaviour; different suites run in parallel.
# A mis-grouping can only make a consumer LOSE its producer -> that consumer goes RED (the verdict diff catches
# it), NEVER a false GREEN (an isolated clean dir cannot make a test pass that should fail). Refine groupings only
# with the diff green.
#
# Output: a sorted "TEST: VERDICT" list (same vocabulary as guard.sh) + a final ALL GREEN / regression summary.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export ROOT
CLI="src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll"
JOBS="${JOBS:-$(nproc)}"
TMP="${TMPDIR:-/tmp}"
T0=$(date +%s); el() { echo "[+$(( $(date +%s) - T0 ))s] $*"; }

el "=== Building (CLI + test projects) ==="
dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj -v quiet
dotnet build tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj -v quiet
dotnet build tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj -v quiet

el "=== Unit + Integration tests (parallel, --no-build) ==="
dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --no-build --verbosity quiet \
    > "$TMP/gf_unit.log" 2>&1 &
UNIT=$!
dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --no-build --verbosity quiet \
    > "$TMP/gf_int.log" 2>&1 &
INT=$!

el "=== NIST: parallel compile + grouped parallel run (JOBS=$JOBS) ==="
# Authoritative test list — extracted from guard.sh's NIST_TESTS so the two never drift.
TESTS=$(sed -n '/^NIST_TESTS="/,/^"/p' scripts/guard.sh | grep -vE '^NIST_TESTS=|^"$' | tr '\n' ' ')

OUT="tests/nist/output"
mkdir -p "$OUT"
# Clear stale compiled output first so a compile FAILURE leaves NO .dll -> the run reports COMPILE FAILED instead
# of silently running a previous run's binary (a stale-dll false green).
rm -f "$OUT"/*.dll "$OUT"/*.runtimeconfig.json "$OUT"/*.txt
cp src/CobolSharp.Runtime/bin/Debug/net10.0/CobolSharp.Runtime.dll "$OUT/"

# (1) Parallel compile — fully independent (distinct .dll/.runtimeconfig.json per test, no shared run state).
echo "$TESTS" | tr ' ' '\n' | grep . \
  | xargs -P"$JOBS" -I {} bash -c 'dotnet "'"$CLI"'" --nist "tests/nist/programs/{}.cob" -o "'"$OUT"'/{}.dll" >/dev/null 2>&1 || true'

# (2) Group tests for isolation. Three tiers (verified equivalent to serial by scripts/guard-verify.sh — a missed
# chain shows as a consumer DIFF, never a false green):
#   WHOLE  — a whole suite runs SERIALLY in one dir (dense producer/consumer chains; not worth splitting).
#   CHAINS — an explicit small ordered producer→consumer group (so the rest of its suite can be singletons).
#   else   — a SINGLETON: its own scratch dir, fully parallel (self-contained tests).
# SQ is mostly self-contained, so it runs as singletons + the one verified SQ202A→203A→204A chain — this splits
# the former 83-test SQ serial bottleneck across cores. IX has dense producer/consumer chains (IX111A/IX119A read
# files from non-adjacent producers), so it stays whole-suite. NC/IF/RW do no cross-test file I/O.
# FUTURE (to go below ~3min): the floor is per-test `dotnet` cold-start (~4s) × the longest serial group, so the
# wins are (a) carve the real IX/ST/RL chains out as small CHAINS (rest singletons; the verdict-diff makes this
# safe), and (b) a persistent run-host to amortize cold-start. Each must keep scripts/guard-verify.sh green.
# NOTE: do NOT name the array GROUPS — that collides with bash's built-in $GROUPS array.
WHOLE="IC RL ST SM IX OB"
CHAINS=(
    "SQ202A SQ203A SQ204A"
)
declare -A GRP
declare -A INCHAIN
SPEC="$TMP/gf_groups.txt"; : > "$SPEC"
ci=0
for chain in "${CHAINS[@]}"; do
    printf 'chain%d|%s\n' "$ci" "$chain" >> "$SPEC"
    for t in $chain; do INCHAIN["$t"]=1; done
    ci=$((ci+1))
done
for test in $TESTS; do
    [ -n "${INCHAIN[$test]:-}" ] && continue          # already in an explicit chain
    suite="${test:0:2}"
    case " $WHOLE " in *" $suite "*) GRP["$suite"]+="$test " ;; *) GRP["$test"]+="$test " ;; esac
done
for gid in "${!GRP[@]}"; do
    printf '%s|%s\n' "$gid" "${GRP[$gid]}"
done >> "$SPEC"

# (3) Run groups in parallel; each group is serial-in-its-own-dir via guard-run-group.sh.
RESULTS="$TMP/gf_results.txt"
xargs -P"$JOBS" -a "$SPEC" -I LINE bash -c \
  'line="LINE"; bash "$ROOT/scripts/guard-run-group.sh" "$ROOT" "${line%%|*}" "${line#*|}"' \
  > "$RESULTS" 2>/dev/null

el "=== NIST run complete ==="
sort "$RESULTS"
NIST_FAILS=$(grep -cE "REGRESSION!" "$RESULTS" || true); NIST_FAILS=${NIST_FAILS:-0}
NIST_MATCH=$(grep -cE ": MATCH" "$RESULTS" || true); NIST_MATCH=${NIST_MATCH:-0}
echo "=== NIST: ${NIST_MATCH} MATCH, ${NIST_FAILS} REGRESSION(S) ==="

# (4) Wait for the .NET test runs.
wait "$UNIT"; UNIT_RC=$?
wait "$INT"; INT_RC=$?
echo "=== Unit ==="; grep -E "Passed!|Failed!|error" "$TMP/gf_unit.log" | tail -2
echo "=== Integration ==="; grep -E "Passed!|Failed!|error" "$TMP/gf_int.log" | tail -2

# (5) Baseline-cleanliness check (parity with guard.sh): no 0-byte / FAIL* / nonzero-footer baselines.
BASE_FAILS=0
for f in tests/nist/valid/*.txt; do
    [ -s "$f" ] || { echo "=== ERROR: $(basename "$f") is EMPTY ==="; BASE_FAILS=$((BASE_FAILS+1)); continue; }
    fc=$(grep -c "FAIL\*" "$f" 2>/dev/null || true); fc=${fc:-0}
    [ "$fc" -gt 0 ] 2>/dev/null && { echo "=== ERROR: $(basename "$f") has $fc FAIL* ==="; BASE_FAILS=$((BASE_FAILS+1)); }
    ff=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$f" 2>/dev/null | grep -oE "^[0-9]+" | head -1); ff=${ff:-0}
    [ "$ff" -gt 0 ] 2>/dev/null && { echo "=== ERROR: $(basename "$f") footer $ff TEST(S) FAILED ==="; BASE_FAILS=$((BASE_FAILS+1)); }
done

if [ "$NIST_FAILS" -eq 0 ] && [ "$UNIT_RC" -eq 0 ] && [ "$INT_RC" -eq 0 ] && [ "$BASE_FAILS" -eq 0 ]; then
    echo "=== ALL GREEN ==="
    exit 0
fi
echo "=== FAILURES: nist=$NIST_FAILS unit_rc=$UNIT_RC int_rc=$INT_RC baselines=$BASE_FAILS ==="
exit 1
