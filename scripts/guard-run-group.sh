#!/bin/bash
# Run ONE NIST group serially in an isolated scratch directory, emitting one "TEST: VERDICT" line per test to
# stdout. Used by scripts/guard-fast.sh to parallelize the NIST suite across cores with airtight isolation.
#
#   $1 = repo root (absolute)
#   $2 = group id (informational)
#   $3 = ordered, space-separated test names that must share a dir + run order (a producer/consumer chain, or a
#        single self-contained test)
#
# Mirrors scripts/guard.sh's per-test run+compare logic EXACTLY (same report resolution, same written-files arm, same
# FAIL*/footer rules); only the working directory differs.
# ⭐ THAT IS NO LONGER A PROMISE KEPT BY HAND: both guards call scripts/guard-verdict.sh, the ONE implementation
# of the evidence rules (`feedback_one_rule_one_place`). The prose used to say the two blocks were kept
# "character-for-character in step", and they had already drifted (this file's normalize() carried a `[ -f ]`
# guard guard.sh's did not) while sharing every hole kb/Work/PB473 found.
# ⛔ EVERY PROGRAM RUNS IN ITS OWN CLEAN DIRECTORY, AFTER EXACTLY ITS DECLARED PREDECESSORS (kb/Work PB3334, review
# round 2) — NistRunner's rule, so the two arms grade a program over the same input. Its predecessors are corpus.tsv's
# chain-preds column (guard_chain_preds), run in order and ungraded (each is graded as its own member of the group).
# The group's programs used to share ONE directory, so a consumer ran over whatever the members before it left: IX115A,
# which declares only IX109A, ran over the tf024.txt that IX110A and IX114A had opened I-O. That agreed with NistRunner
# only while an OPEN and a CLOSE that changed no record left the file's bytes alone. The keyed-store engine's writable
# OPEN writes its fence (DESIGN-runtime-library 2.2.1.6, R6 step 4), so IX114A-IX120A wrote a tf024.txt that differed
# from the golden the C# arm captured over IX109A's file alone.
#
# ⛔ THE EVIDENCE RULES (plan §11 A12b/A12d; DESIGN-test-build-ci.md §3.10) live in scripts/guard-verdict.sh and
# cover all THREE arms — compile, run and compare. Read that file's header for what each one requires and why.
set -u

ROOT="$1"; GROUP="$2"; TESTS="$3"
OUT="$ROOT/tests/nist/output"
# The COBOL runtime the compiled programs bind to, `Cobol.Net.Runtime` (scripts/guard-compiler.sh selects it).
# guard-fast.sh exports GUARD_RUNTIME_DLL (a repo-relative path); the default keeps a standalone invocation —
# and scripts/guard-verify.sh's witnesses — working without the caller.
RUNTIME="${GUARD_RUNTIME_DLL:-src/Cobol.Net.Runtime/bin/Debug/net10.0/Cobol.Net.Runtime.dll}"
case "$RUNTIME" in /*|[A-Za-z]:[\\/]*) ;; *) RUNTIME="$ROOT/$RUNTIME" ;; esac
# A hung program used to block its whole group forever; the timeout makes that a reported NO-VERDICT instead.
# `timeout` reports 124 on expiry and 137 after the -k KILL. A COBOL program may itself STOP RUN with status
# 124 (§14.9.42.4 GR5), but that ambiguity only arises on a NON-MATCHING run, where declining to score is the
# conservative direction — and it is loud, so it gets read rather than absorbed.
RUN_TIMEOUT="${GUARD_RUN_TIMEOUT:-120}"

# THE evidence rules (compile · run · compare · termination), shared with scripts/guard.sh, and the manifest's
# one shell reader (guard_declared_termination).
. "$(cd "$(dirname "$0")" && pwd)/guard-verdict.sh"
. "$(cd "$(dirname "$0")" && pwd)/guard-population.sh"

# One clean run directory per program under $RUNS (its own $WORK, below).
RUNS="$(mktemp -d)"
# The comparison's scratch — DELIBERATELY OUTSIDE every run directory. The normalized copies must never become
# inputs to a program (IX216A/IX217A are canaries for any stray file).
CMP="$RUNS.cmp"
mkdir -p "$CMP"
trap 'rm -rf "$RUNS" "$CMP"' EXIT
export COBOL_SWITCH_1=ON

# run_predecessor PRED CONSUMER — run PRED, ungraded, in CONSUMER's run directory ($WORK), its standard input its data
# file as when it is graded. A predecessor with no compiled program is said so on stderr and skipped: the consumer
# then lacks its input and goes red, never a false green.
run_predecessor() {
    local pred="$1" consumer="$2" pdata="$ROOT/tests/nist/data/$1.dat" prc=0
    if [ ! -f "$OUT/$pred.dll" ]; then
        echo "  ⚠ $consumer: its declared predecessor $pred has no compiled program — $consumer runs without it" >&2
        return 0
    fi
    cp "$OUT/$pred.dll" "$WORK/"
    [ -f "$OUT/$pred.runtimeconfig.json" ] && cp "$OUT/$pred.runtimeconfig.json" "$WORK/"
    if [ -f "$pdata" ]; then
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$pred.dll") < "$pdata" > /dev/null 2>&1 || prc=$?
    else
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$pred.dll") > /dev/null 2>&1 || prc=$?
    fi
    if [ "$prc" -eq 124 ] || [ "$prc" -eq 137 ]; then
        echo "  ⚠ $consumer: its declared predecessor $pred timed out (rc=$prc) in $consumer's directory" >&2
    fi
    return 0
}

for test in $TESTS; do
    dll="$OUT/$test.dll"
    if [ ! -f "$dll" ]; then
        # EVIDENCE RULE (compile): "no .dll" is not a compiler verdict on its own — see guard-verdict.sh.
        clog="$OUT/$test.compile.log"
        crc="$(cat "$OUT/$test.compile.rc" 2>/dev/null || echo "")"
        guard_compile_verdict "$test" "$crc" "$clog"
        echo "$test: $GUARD_VERDICT"
        continue
    fi
    WORK="$RUNS/$test"
    mkdir -p "$WORK"
    cp "$RUNTIME" "$WORK/"
    preds="$(guard_chain_preds "$test")" || exit 1
    for pred in $preds; do run_predecessor "$pred" "$test"; done
    cp "$dll" "$WORK/"
    [ -f "$OUT/$test.runtimeconfig.json" ] && cp "$OUT/$test.runtimeconfig.json" "$WORK/"

    outfile="$(echo "$test" | tr '[:upper:]' '[:lower:]').txt"
    # The program's stdout and stderr live OUTSIDE its run directory ($CMP): the written-files arm (kb/Work PB804)
    # grades what the program wrote INTO $WORK, and the shell's own redirections are not the program's output.
    stdoutfile="$CMP/${test}-stdout.txt"
    errfile="$CMP/${test}-stderr.txt"
    marker="$CMP/run.marker"
    datafile="$ROOT/tests/nist/data/$test.dat"
    # EVIDENCE RULE (run): keep the exit status and stderr instead of discarding both with `|| true`. They are
    # what distinguishes "the program produced the wrong answer" from "the program never finished".
    rrc=0
    : > "$marker"     # what the program WRITES is what is newer than this (guard_files_apply); after the copies above
    if [ -f "$datafile" ]; then
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") < "$datafile" > "$stdoutfile" || rrc=$?
    else
        (cd "$WORK" && timeout -k 5 "$RUN_TIMEOUT" dotnet "$test.dll" 2>"$errfile") > "$stdoutfile" || rrc=$?
    fi

    validfile="$ROOT/tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        fc=$(grep -c "FAIL\*" "$WORK/$outfile" 2>/dev/null || true); fc=${fc:-0}
        echo "$test: NO BASELINE (${fc} FAIL* — pending fix)"
        continue
    fi

    # A row corpus.tsv declares TERMINATES (kb/Work PB1955): the run unit's end on the named exception is the
    # observation, not the report. guard-fast.sh exports GUARD_TERMINATES.
    ec="$(guard_declared_termination "$test")"
    if [ -n "$ec" ]; then
        guard_termination_verdict "$test" "$ec" "$rrc" "$errfile" "$RUN_TIMEOUT"
        # ⛔ The files the run left at its abnormal end are graded too (kb/Work PB804): report name "" = no report.
        guard_files_apply "$test" "$ROOT/tests/nist/valid-files/$test" "$WORK" "$marker" "" "$stdoutfile" "$CMP/files.problems"
        echo "$test: $GUARD_VERDICT"
        if [ "$GUARD_CLASS" != "match" ]; then
            guard_preserve "$test" "$GUARD_VERDICT" "$WORK/$outfile" "$stdoutfile" "$errfile" "$CMP/files.problems"
        fi
        continue
    fi

    # RUN + COMPARE arms, scored by the shared evidence rules: the program's report (guard_report_file), then
    # ⛔ EVERY OTHER FILE THE PROGRAM WROTE (kb/Work PB804) once the report matched.
    guard_output_verdict "$test" "$validfile" "$rrc" "$errfile" "$RUN_TIMEOUT" "$CMP" \
        "$(guard_report_file "$WORK" "$outfile" "$stdoutfile")"
    guard_files_apply "$test" "$ROOT/tests/nist/valid-files/$test" "$WORK" "$marker" "$outfile" "$stdoutfile" "$CMP/files.problems"
    echo "$test: $GUARD_VERDICT"
    if [ "$GUARD_CLASS" != "match" ]; then
        # Anything but a MATCH is worth a post-mortem, and this dir dies with the group. Keep the evidence.
        guard_preserve "$test" "$GUARD_VERDICT" \
            "$WORK/$outfile" "$stdoutfile" "$errfile" \
            "$CMP/expected.norm" "$CMP/actual.norm" "$CMP/files.problems"
    fi
done
