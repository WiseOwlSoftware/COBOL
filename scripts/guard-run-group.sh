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
#
# ⛔ THE EVIDENCE RULES (plan §11 A12b/A12d; DESIGN-test-build-ci.md §3.10). A verdict about the COMPILER is
# produced ONLY from an observation this script actually made. The defect these close is one sentence:
# A MISSING OBSERVATION WAS BEING READ AS A NEGATIVE OBSERVATION.
#   · COMPILE — a missing .dll used to BE the verdict "COMPILE FAILED". It is not: at -P32 a transient (host
#     startup, memory pressure, a file lock) leaves no .dll and is indistinguishable from a real syntax error,
#     and the compiler's own diagnostics — which settle it instantly — were being sent to /dev/null. A compile
#     is now FAILED only with a non-zero rc AND diagnostic text; anything else is COMPILE NO-VERDICT.
#   · RUN — the run's exit status was discarded by `|| true`, so a program killed or timed out mid-write left a
#     TRUNCATED report that was then scored `DIFF — REGRESSION!`. That manufactures a regression out of a lost
#     result. A non-match is now scored only when the process RAN TO COMPLETION; otherwise RUN NO-VERDICT.
# A NO-VERDICT is never MATCH and never REGRESSION — it is an explicit failure of the run to observe anything,
# and scripts/guard-nist-audit.sh treats it as a finding.
set -u

ROOT="$1"; GROUP="$2"; TESTS="$3"
OUT="$ROOT/tests/nist/output"
RUNTIME="$ROOT/src/CobolSharp.Runtime/bin/Debug/net10.0/CobolSharp.Runtime.dll"
# A hung program used to block its whole group forever; the timeout makes that a reported NO-VERDICT instead.
# `timeout` reports 124 on expiry and 137 after the -k KILL. A COBOL program may itself STOP RUN with status
# 124 (§14.9.42.4 GR5), but that ambiguity only arises on a NON-MATCHING run, where declining to score is the
# conservative direction — and it is loud, so it gets read rather than absorbed.
RUN_TIMEOUT="${GUARD_RUN_TIMEOUT:-120}"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cp "$RUNTIME" "$WORK/"
export COBOL_SWITCH_1=ON

# tr -d '\r' FIRST (CRLF golden vs LF program output on Linux); must precede 's/ *$//' which is a no-op while a
# trailing \r remains. Idempotent on Windows. See the matching note in scripts/guard.sh.
# ⚠ The `[ -f ]` guard is load-bearing, not defensive tidiness. The fallback candidates (print-file.txt, the
# lowercase report) legitimately do not exist for most tests, and `tr < "$1" 2>/dev/null` does NOT suppress the
# resulting message: bash applies the INPUT redirection before `2>/dev/null` takes effect, so the shell's "No
# such file or directory" escapes to stderr. That was invisible while the group runner's stderr went to
# /dev/null; now that guard-fast CAPTURES that channel (so a group dying can never again be silent), the noise
# would drown the signal — and a channel people learn to ignore is no better than one nobody reads.
normalize() { [ -f "$1" ] || return 0; tr -d '\r' < "$1" | sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/'; }

for test in $TESTS; do
    dll="$OUT/$test.dll"
    if [ ! -f "$dll" ]; then
        # EVIDENCE RULE (compile): "no .dll" is not a compiler verdict on its own — see the header.
        clog="$OUT/$test.compile.log"
        crc="$(cat "$OUT/$test.compile.rc" 2>/dev/null || echo "")"
        if [ -n "$crc" ] && [ "$crc" != "0" ] && [ -s "$clog" ]; then
            echo "$test: COMPILE FAILED — REGRESSION! (rc=$crc: $(head -1 "$clog" | tr -d '\r' | cut -c1-110))"
        elif [ -z "$crc" ]; then
            echo "$test: COMPILE NO-VERDICT (the compile never reported an exit status) — NOT SCORED"
        elif [ "$crc" = "0" ]; then
            echo "$test: COMPILE NO-VERDICT (rc=0 but no .dll — contradictory evidence) — NOT SCORED"
        else
            echo "$test: COMPILE NO-VERDICT (rc=$crc with NO diagnostic — a rejection with no reason is a lost result) — NOT SCORED"
        fi
        continue
    fi
    cp "$dll" "$WORK/"
    [ -f "$OUT/$test.runtimeconfig.json" ] && cp "$OUT/$test.runtimeconfig.json" "$WORK/"

    outfile="$(echo "$test" | tr '[:upper:]' '[:lower:]').txt"
    stdoutfile="$WORK/${test}-stdout.txt"
    errfile="$WORK/${test}-stderr.txt"
    datafile="$ROOT/tests/nist/data/$test.dat"
    # EVIDENCE RULE (run): keep the exit status and stderr instead of discarding both with `|| true`. They are
    # what distinguishes "the program produced the wrong answer" from "the program never finished".
    rrc=0
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

    # An ISO-re-baselined golden the legacy legitimately diverges from (the list lives in guard.sh; guard-fast
    # exports it): compiled and ran above; the diff is expected — never a regression.
    case " ${LEGACY_DIVERGENT:-} " in *" $test "*)
        echo "$test: LEGACY DIVERGENT (golden = ISO-conforming baseline; expected diff)"
        continue ;;
    esac

    actual=""
    if   diff <(normalize "$validfile") <(normalize "$WORK/$outfile")        >/dev/null 2>&1; then actual="$WORK/$outfile"
    elif diff <(normalize "$validfile") <(normalize "$WORK/print-file.txt")  >/dev/null 2>&1; then actual="$WORK/print-file.txt"
    elif diff <(normalize "$validfile") <(normalize "$stdoutfile")           >/dev/null 2>&1; then actual="$stdoutfile"
    fi

    if [ -z "$actual" ]; then
        # Nothing matched. Before calling that a REGRESSION, ask whether the program actually ran — a killed or
        # timed-out process leaves a TRUNCATED report that diffs exactly like a wrong answer does.
        if [ "$rrc" -eq 124 ] || [ "$rrc" -eq 137 ]; then
            echo "$test: RUN NO-VERDICT (timeout/kill after ${RUN_TIMEOUT}s, rc=$rrc) — NOT SCORED"
        elif [ "$rrc" -gt 128 ]; then
            echo "$test: RUN NO-VERDICT (killed by signal $((rrc - 128))) — NOT SCORED"
        elif [ "$rrc" -ne 0 ]; then
            echo "$test: DIFF — REGRESSION! (run exited $rrc: $(head -1 "$errfile" 2>/dev/null | tr -d '\r' | cut -c1-110))"
        else
            echo "$test: DIFF — REGRESSION!"
        fi
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
