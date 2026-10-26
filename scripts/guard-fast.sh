#!/bin/bash
# THE NIST GUARD — the CLI-level NIST leg. It builds the `cobol` CLI, compiles every program of the population through
# it, runs each program in its OWN clean directory after exactly its declared predecessors (guard-run-group.sh,
# NistRunner's rule) with every core busy, and audits every verdict against tests/nist/corpus.tsv. CI's `guard` job
# and the battery run it; `JOBS=1` runs it serially.
#
# ⛔ ONE GUARD (kb/Work PB3383). A serial twin, scripts/guard.sh, ran the same population in ONE shared directory, in a
# hand-written order (its NIST_TESTS block, kb/Work PB925), so each program met whatever files every program before it
# had left; scripts/guard-verify.sh diffed the two guards' verdicts as an "equivalence proof". Once the keyed-store
# engine's writable OPEN wrote its fence (kb/Work PB3334), IX115A read the tf024.txt IX110A and IX114A had left there and
# went red in the serial guard alone. The serial arm and that proof are retired: the population is DERIVED from the
# manifest (guard_nist_population, scripts/guard-population.sh), and the audit in step (3c) is the check the proof
# stood for, made ABSOLUTE — every verdict against corpus.tsv, where two guards compared with each other could only
# ever agree or disagree.
#
# WHY THIS IS SAFE (isolation model): NIST programs couple ONLY through shared on-disk data files (TF### producer/
# consumer chains), and that coupling is DECLARED — `tests/nist/corpus.tsv`'s `chain-preds` column. Every program is
# its own job (step 2): its directory holds what its declared predecessors wrote and nothing any other program wrote,
# so no ORDER and no GROUPING carries a chain, and a non-predecessor cannot touch a consumer's file at all. The
# GREENFIELD leg (`NistDifferentialTests`) runs every program by the same rule, in-process.
# A missing predecessor edge can only make a consumer LOSE its input -> that consumer goes RED, NEVER a false GREEN (a
# clean directory cannot make a test pass that should fail) — and step (3c)'s audit names any program whose verdict
# differs from what the manifest predicts.
#
# Output: a sorted "TEST: VERDICT" list, the NIST AUDIT, and ALL GREEN / failures.
# ⛔ READ THE AUDIT LINE, NOT THE MATCH COUNT. "353 MATCH" is a number a human compares against memory, which is
# exactly how a run at 352 once passed as green.
#
# ⛔ AND READ WHICH COMPILER IT DROVE. Until 2026-09-06 this leg hard-coded the legacy byte engine's CLI, so its
# headline MATCH count was a statement about the oracle (kb/Work/PB750). It drives `cobol` (src/WiseOwl.Cobol.Cli),
# refuses to start unless the resolved binary really references `WiseOwl.Cobol.Compiler`, and NAMES the compiler in
# every summary line: `=== NIST (cobol): ... ===`.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export ROOT

# WHICH COMPILER, asserted against the binary's own dependency graph (scripts/guard-compiler.sh).
. "$(dirname "$0")/guard-compiler.sh"
guard_select_compiler
guard_announce_compiler
CLI="$GUARD_CLI_DLL"
JOBS="${JOBS:-$(nproc)}"
# ⛔ FULL FAN-OUT IS KEPT, AND THE LOST OBSERVATIONS ARE RE-TAKEN INSTEAD (plan §11 A12b/A12d).
# Five runs on ONE unchanged tree gave five different answers, and the cause is CONTENTION at -P$(nproc) — every
# affected program compiled and ran clean serially. The obvious response, halving the fan-out, is the WRONG one:
# it slows every run to protect against something that is now DETECTED rather than mis-scored. With the evidence
# rules in guard-run-group.sh in place, contention can no longer corrupt a verdict — it can only LOSE one, and a
# lost observation is re-taken (step 3b below) at full serial isolation. Cost is proportional to the damage
# instead of paid up front on every run.
# Set CJOBS/RJOBS to throttle deliberately on a small machine; the default is every core.
CJOBS="${CJOBS:-$JOBS}"
RJOBS="${RJOBS:-$JOBS}"
# This run's OWN scratch directory (kb/Work PB2922): fixed names (gf_population.txt, gf_results.txt, ...) directly in the
# machine-wide temp directory were one file for every guard run on the host. It goes when the run ends, unless it holds
# the forensics of a non-MATCH (below), which the verdict names.
TMP="$(mktemp -d "${TMPDIR:-/tmp}/guard-fast.XXXXXX")"
gf_cleanup() { [ -d "${GUARD_FORENSICS:-}" ] && [ "${GUARD_FORENSICS#"$TMP"/}" != "$GUARD_FORENSICS" ] || rm -rf "$TMP"; }
trap gf_cleanup EXIT
T0=$(date +%s); el() { echo "[+$(( $(date +%s) - T0 ))s] $*"; }

# ⛔ THE GUARD'S OWN INSTRUMENTS FIRST, every run (kb/Work PB1955). The population reader, the verdict audit and
# the evidence-rule witnesses each carry a self-test that proves its checks can FAIL, and until 2026-10-04 no gate
# ran any of them, so a change to how corpus.tsv is read reached CI unexercised. Seconds, no compiler, no corpus.
el "=== Guard instruments' self-tests (population reader, verdict audit, evidence-rule witnesses) ==="
SELF_RC=0
for selftest in "scripts/guard-population.sh --self-test" "scripts/guard-nist-audit.sh --self-test" \
                "scripts/guard-verify.sh --self-test"; do
    # shellcheck disable=SC2086 # the entry is a script and its one argument
    if ! bash $selftest > "$TMP/gf_selftest.log" 2>&1; then
        echo "  RED: bash $selftest — its complete log:"; sed 's/^/    /' "$TMP/gf_selftest.log"; SELF_RC=1
    fi
done
[ "$SELF_RC" -eq 0 ] && echo "  all three GREEN"

el "=== Building ($GUARD_COMPILER CLI) ==="
dotnet build "$GUARD_CLI_PROJECT" -v quiet

# ⛔ THE IDENTITY WATCHDOG, AFTER THE BUILD AND BEFORE ANY MEASUREMENT (kb/Work/PB750). A gate that cannot say
# which compiler it drove is not a gate; a gate that drives the wrong one is worse, because it is green.
guard_assert_compiler_identity "$CLI" || exit 1

el "=== NIST: parallel compile + per-program parallel run (JOBS=$JOBS compile=$CJOBS run=$RJOBS) ==="
# ⛔ THE POPULATION AND THE TERMINATES ROWS, DERIVED from tests/nist/corpus.tsv through scripts/guard-population.sh,
# the ONE shell reader of the manifest (kb/Work PB925, PB898, PB1955), which guard-nist-audit.sh asks too.
. "$(dirname "$0")/guard-population.sh"
# The population: every row the golden suite asserts, plus its declared predecessors (guard_nist_population). It is
# written to a file so the audit asserts against it rather than against a remembered count.
POP="$TMP/gf_population.txt"
guard_nist_population > "$POP" || exit 1
# GUARD_TESTS overrides the population with a subset, for exercising this guard's own branches or a change that can
# reach only those programs. The audit still applies in full: it takes the population as an argument. ⚠ A subset run
# is NOT the regression gate; it proves the loop, not the corpus.
if [ -n "${GUARD_TESTS:-}" ]; then
    el "  ⚠ GUARD_TESTS override in effect — running a SUBSET, not the regression gate: $GUARD_TESTS"
    echo "$GUARD_TESTS" | tr ' ' '\n' | grep . | LC_ALL=C sort -u > "$POP"
fi
# Every program the run compiles: the population, plus each member's declared predecessors, which run in its
# directory (guard-run-group.sh) from the program compiled here. Over the whole population the two lists are one (a
# predecessor of a member is a member); under GUARD_TESTS a consumer's predecessors are compiled though not graded.
COMPILE="$TMP/gf_compile.txt"
guard_with_predecessors < "$POP" > "$COMPILE" || exit 1
# A TERMINATES row's golden records a run that continued past a fatal I-O status nothing covers, which WiseOwl
# COBOL's Annex A.1 item 103 choice ends, so the program runner checks that the run ENDS naming the declared exception
# (GUARD_TERMINATES, `NAME=EC-…`). Every other program, a `divergent` one included, is compared with its golden (those
# goldens are the ISO-conforming output, which NistDifferentialTests also locks byte-exact).
GUARD_TERMINATES="$(guard_terminating)" || exit 1
export GUARD_TERMINATES
export GUARD_RUNTIME_DLL

OUT="tests/nist/output"
mkdir -p "$OUT"
# Clear stale compiled output first so a compile FAILURE leaves NO .dll -> the run reports COMPILE FAILED instead
# of silently running a previous run's binary (a stale-dll false green).
# ⚠ `*.g.cs` is a greenfield artefact and must be cleaned with everything else: `cobol` writes the generated
# C# next to each output assembly, where a stale copy would mislead a post-mortem. (`*.cbattr` used to be swept
# here too — the fixed-file-attribute catalog sidecar. It no longer exists: owner decision 2026-09-07,
# kb/Work PB802, put a keyed file's §9.1.6 attributes in the store's own header, so nothing outlives the data
# file the `*.txt` sweep removes.)
rm -f "$OUT"/*.dll "$OUT"/*.runtimeconfig.json "$OUT"/*.txt "$OUT"/*.compile.log "$OUT"/*.compile.rc \
      "$OUT"/*.g.cs
cp "$GUARD_RUNTIME_DLL" "$OUT/"

# (1) Parallel compile — fully independent (distinct .dll/.runtimeconfig.json per test, no shared run state).
# ⛔ THE DIAGNOSTICS ARE KEPT (plan §11 A12b). They used to go to /dev/null and the verdict was inferred from
# whether a .dll existed, so a transient and a genuine syntax error reported IDENTICALLY and the one thing that
# would have settled it was thrown away. Each compile now records its own log and its own exit status; the
# program runner reads both, and refuses to call a compile FAILED without evidence.
export GUARD_CLI_DLL="$CLI"
xargs -P"$CJOBS" -a "$COMPILE" -I {} bash "$ROOT/scripts/guard-compile.sh" {} "tests/nist/programs/{}.cob" "$OUT"

# (2) EVERY PROGRAM IS ITS OWN JOB. Its runner (guard-run-group.sh) gives it a clean directory and runs its declared
# predecessors there first, so a chain needs no grouping and no order (kb/Work PB3334, PB3383). The jobs used to be
# the connected components of the chain graph, each run serially in one directory, and before that six whole suites;
# both were the coupling's over-approximations, and the longest group was every run's floor (an IX chain of nine
# programs walked one at a time while the other cores idled). The declared graph is proven complete by the GREENFIELD
# leg, `NistDifferentialTests`, which runs every program by the same rule. A MISSING predecessor edge still shows as a
# consumer DIFF, never a false green, and the audit in step (3c) names it against the manifest.
# (3) Run them in parallel.
# ⛔ THE RUNNER'S STDERR IS KEPT. It used to go to /dev/null, which is how a runner could die outright and take its
# programs' verdict lines with it in total silence — the mechanism behind the SQ135A false green.
RESULTS="$TMP/gf_results.txt"
GROUPERR="$TMP/gf_group_stderr.txt"
# ⛔ A NON-MATCH'S EVIDENCE OUTLIVES ITS RUN (kb/Work/PB473 item 4). Every program's work dir is a mktemp its runner's
# own EXIT trap deletes, so battery #43's one red had nothing left to examine and attributing it took hours.
# Run-scoped, and written to ONLY when a program scores something other than MATCH.
GUARD_FORENSICS="${GUARD_FORENSICS:-$TMP/gf_forensics}"
export GUARD_FORENSICS
rm -rf "$GUARD_FORENSICS"
xargs -P"$RJOBS" -a "$POP" -I {} bash "$ROOT/scripts/guard-run-group.sh" "$ROOT" {} {} > "$RESULTS" 2>"$GROUPERR"

el "=== NIST run complete ==="
sort "$RESULTS"
if [ -s "$GROUPERR" ]; then
    echo "=== PROGRAM RUNNER STDERR (was previously discarded — read it) ==="
    head -40 "$GROUPERR"
fi
# (3b) ⛔ RE-OBSERVE THE LOST RESULTS, SERIALLY — the half that lets step (1)/(3) keep every core.
# A NO-VERDICT means the harness observed NOTHING about that program: a compile that produced neither a .dll
# nor a diagnostic, or a run that was killed before it finished. Nothing was learned, so re-taking the
# measurement discards no result — it is not re-rolling a failed assertion. The retry recompiles each lost program
# and its declared predecessors and re-runs it, AND every member that declares a lost program as its predecessor (a
# predecessor whose compile was lost left its consumers to run without it), with no other work in flight, which is
# the quietest machine this run can offer. Anything still unobserved after that stays a NO-VERDICT and the audit
# fails on it.
# ⛔ A PROGRAM WITH NO LINE AT ALL IS LOST TOO (battery-8, 2026-08-18): SQ201M's single-program group emitted
# neither a verdict nor a byte of stderr, the audit reported missing=1 — and this step never re-observed it,
# because it re-ran only the programs that had SAID "NO-VERDICT". The population minus the observed programs is
# the other half of "lost", and it is re-taken by the same serial mechanism (a clean serial re-run is what a
# lost result needs; the audit still fails on anything unobserved after that).
# ⚠ NO PROCESS SUBSTITUTION HERE. `comm -23 "$POP" <(…)` reads one side through /dev/fd/N, and a short delivery
# there would silently shrink the "lost" set — the same plumbing that manufactured battery #43's false
# regression (kb/Work/PB473). Both sides are real files whose writes have completed.
REPORTED="$TMP/gf_reported.txt"
sed 's/:.*//' "$RESULTS" | sort -u > "$REPORTED"
LOST=$( { grep -E "NO-VERDICT" "$RESULTS" | sed 's/:.*//'; comm -23 "$POP" "$REPORTED"; } | sort -u | tr '\n' ' ')
if [ -n "${LOST// /}" ]; then
    el "=== NIST: re-observing $(echo "$LOST" | wc -w) lost result(s) SERIALLY: $LOST ==="
    RETRY="$TMP/gf_retry.txt"; : > "$RETRY"
    RETRY_RESULTS="$TMP/gf_retry_results.txt"; : > "$RETRY_RESULTS"
    while IFS= read -r t; do
        preds=" $(guard_chain_preds "$t") "
        for l in $LOST; do
            if [ "$t" = "$l" ]; then echo "$t" >> "$RETRY"; break; fi
            case "$preds" in *" $l "*) echo "$t" >> "$RETRY"; break ;; esac
        done
    done < "$POP"
    # Recompile them and their predecessors serially, keeping the diagnostics, then re-run them one at a time.
    guard_with_predecessors < "$RETRY" > "$TMP/gf_retry_compile.txt" || exit 1
    while IFS= read -r t; do
        bash "$ROOT/scripts/guard-compile.sh" "$t" "tests/nist/programs/$t.cob" "$OUT"
    done < "$TMP/gf_retry_compile.txt"
    # ⚠ stderr goes to the RUNNER ERROR file, never into the results: merging it with 2>&1 would inject
    # non-verdict lines into the verdict file, which the audit would then report as STRAY verdicts.
    bash "$ROOT/scripts/guard-run-group.sh" "$ROOT" retry "$(tr '\n' ' ' < "$RETRY")" >> "$RETRY_RESULTS" 2>>"$GROUPERR"
    # Replace the affected programs' lines in $RESULTS with the re-observed ones.
    if [ -s "$RETRY_RESULTS" ]; then
        RETRIED=$(sed 's/:.*//' "$RETRY_RESULTS" | sort -u)
        grep -vE "^($(echo "$RETRIED" | tr '\n' '|' | sed 's/|$//')): " "$RESULTS" > "$TMP/gf_kept.txt" || true
        cat "$TMP/gf_kept.txt" "$RETRY_RESULTS" | sort > "$RESULTS"
        rm -f "$RETRY_RESULTS"
        echo "=== after re-observation ==="; sort "$RESULTS"
    fi
fi

NIST_FAILS=$(grep -cE "REGRESSION!" "$RESULTS" || true); NIST_FAILS=${NIST_FAILS:-0}
NIST_MATCH=$(grep -cE ": MATCH" "$RESULTS" || true); NIST_MATCH=${NIST_MATCH:-0}
NIST_TERM=$(grep -cE ": TERMINATES " "$RESULTS" || true); NIST_TERM=${NIST_TERM:-0}
# ⛔ THE LINE NAMES THE COMPILER (kb/Work/PB750). "NIST: 353 MATCH" was quoted in plan §9 battery records as
# evidence about WiseOwl COBOL for months while it measured the legacy oracle; a verdict that does not say what it
# measured is a verdict a reader will attribute to whatever they were thinking about.
echo "=== NIST (${GUARD_COMPILER}): ${NIST_MATCH} MATCH, ${NIST_TERM} TERMINATES (declared), ${NIST_FAILS} REGRESSION(S) ==="

# ⛔ (3c) THE POPULATION + EXPECTATION AUDIT — plan §11 A12c. The two counts above are NOT a verdict: they are
# computed from the lines that ARRIVED, so losing a program lowers MATCH and still reads as green. The audit
# asserts that every declared program produced exactly one verdict and that it is the verdict the committed
# manifest predicts. It is the half that makes a false GREEN impossible.
bash scripts/guard-nist-audit.sh "$RESULTS" "$POP"
NIST_AUDIT=$?

# (4) Baseline-cleanliness check — ONE implementation (scripts/guard-baselines.sh), which guard-verify.sh's
# self-test fires arm by arm.
bash scripts/guard-baselines.sh tests/nist/valid tests/nist/corpus.tsv
BASE_FAILS=$?

# The verdict says what it measured — `=== ALL GREEN ===` itself is left byte-identical because callers
# (scripts/battery.sh, CI) gate on that exact token.
echo "=== COMPILER UNDER TEST WAS: $GUARD_COMPILER ($CLI) ==="
if [ "$NIST_FAILS" -eq 0 ] && [ "$NIST_AUDIT" -eq 0 ] && [ "$BASE_FAILS" -eq 0 ] && [ "$SELF_RC" -eq 0 ]; then
    echo "=== ALL GREEN ==="
    exit 0
fi
echo "=== FAILURES ($GUARD_COMPILER): nist=$NIST_FAILS audit=$NIST_AUDIT baselines=$BASE_FAILS selftests=$SELF_RC ==="
if [ -d "$GUARD_FORENSICS" ]; then
    echo "=== EVIDENCE for every non-MATCH (report, stdout, stderr, both normalized sides): $GUARD_FORENSICS ==="
    ls "$GUARD_FORENSICS"
fi
exit 1
