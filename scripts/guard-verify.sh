#!/bin/bash
# TWO CHECKS, in this order:
#
#  (1) THE EVIDENCE-RULE WITNESSES — synthetic runs through the REAL scripts/guard-run-group.sh, each built so
#      that exactly one evidence rule decides its verdict. Seconds, no corpus, no compiler. A harness whose
#      verdicts are wrong makes (2) meaningless, so this runs first and (2) is skipped if it fails.
#  (2) THE EQUIVALENCE PROOF — run scripts/guard.sh and scripts/guard-fast.sh over the corpus, extract the
#      per-test "NAME: VERDICT" lines, and diff them. They MUST be byte-identical.
#
# Exit 0 iff the witnesses pass AND the verdict lists are identical AND both runs were ALL GREEN.
#   bash scripts/guard-verify.sh              both checks (the full, slow proof)
#   bash scripts/guard-verify.sh --witnesses  the witnesses only (seconds — the wave-local gate for a change to
#                                             the verdict rules themselves)
#
# ⚠ (2) IS NO LONGER THE PRIMARY CHECK, AND THE HEADER USED TO SAY IT WAS. Both guards now run
# scripts/guard-nist-audit.sh, which compares every program's verdict against tests/nist/corpus.tsv — an
# ABSOLUTE check against a committed manifest. (2) is RELATIVE: it compares the two guards to each other, so it
# cannot see them deviating together, and it is necessarily empty whenever both are audit-clean. Keep it for the
# case the audit cannot cover: a difference in the two guards' own MECHANICS that the manifest is blind to
# (working-directory isolation, run ORDER within a group, the compile path).
#
# ⛔ AND "THEY AGREE" WAS NEVER EVIDENCE THAT EITHER IS RIGHT. Both guards shared every hole kb/Work/PB473 found
# in the COMPARE arm, so (2) was perfectly green while a lost observation was being scored `DIFF — REGRESSION!`
# on both sides. That is what (1) is for: it asserts what a verdict MEANS, one rule at a time, and every case
# below was proved RED against the pre-fix runner before it was made green (`feedback_green_gates_arent_evidence`
# — reproduce with `git show 80c87970:scripts/guard-run-group.sh > /tmp/old.sh` and
# `GUARD_GROUP_RUNNER=/tmp/old.sh bash scripts/guard-verify.sh --witnesses`).
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT"

MODE="${1:-all}"
case "$MODE" in
    --witnesses|--witness) MODE="witnesses" ;;
    all|"")                MODE="all" ;;
    *) echo "usage: $0 [--witnesses]" >&2; exit 2 ;;
esac

# The runner under test. Overridable so the witnesses can be pointed at an OLD copy of the group runner and
# shown to fail — a check that has never been shown to fail is not evidence.
GROUP_RUNNER="${GUARD_GROUP_RUNNER:-$ROOT/scripts/guard-run-group.sh}"

# ── (1) THE EVIDENCE-RULE WITNESSES ────────────────────────────────────────────────────────────────────────
# Each case drives the real group runner over a FAKE repo root, with `dotnet` (and, for one case, `diff`)
# replaced by shims on PATH so the scenario is exact and instant: no compiler, no COBOL, no corpus. What the
# program did — wrote a report or not, exited 0 or 1, said something on stderr or nothing — is the entire input,
# which is precisely the evidence the rules are about.
witnesses() {
    local d rc=0 out
    d="$(mktemp -d -t guardwitness.XXXXXX)"
    trap 'rm -rf "$d"' RETURN

    local FR="$d/root" BIN="$d/bin"
    # The runtime the group runner copies beside a program before running it. Since PB750 the guard's default
    # compiler is WiseOwl COBOL, so the default runtime is Cobol.Net.Runtime; the witnesses stub that path.
    mkdir -p "$FR/tests/nist/output" "$FR/tests/nist/valid" "$FR/tests/nist/data" \
             "$FR/src/Cobol.Net.Runtime/bin/Debug/net10.0" "$BIN"
    printf 'stub\n' > "$FR/src/Cobol.Net.Runtime/bin/Debug/net10.0/Cobol.Net.Runtime.dll"

    # The golden: a CCVS-shaped report with a clean footer.
    printf 'FEATURE 1                PASS\nFEATURE 2                PASS\n NO TEST(S) FAILED\n' \
        > "$FR/tests/nist/valid/WT001A.txt"

    # The `dotnet` shim. The group runner invokes it as `timeout -k 5 N dotnet WT001A.dll` with cwd = its own
    # scratch dir, so writing ./wt001a.txt here is exactly what a NIST program does.
    cat > "$BIN/dotnet" <<'SHIM'
#!/bin/bash
t="${1%.dll}"; lt="$(echo "$t" | tr '[:upper:]' '[:lower:]')"      # the program being run: WT001A.dll -> wt001a.txt
case "${WITNESS_CASE:-}" in
    absent)    : ;;                                            # produced nothing at all
    empty)     : > "./$lt.txt" ;;                              # produced a 0-byte report
    correct)   cp "$WITNESS_VALID/$t.txt" "./$lt.txt" ;;
    wrong)     sed 's/PASS/FAIL/' "$WITNESS_VALID/$t.txt" > "./$lt.txt" ;;
    stdout)    cat "$WITNESS_VALID/$t.txt" ;;                  # DISPLAY-only: the report arrives on stdout
esac
# kb/Work PB804: the OTHER files a program writes (WITNESS_FILES="name=content|name=content" — WT001A only, so a second program
# of the group can be shown NOT to have written them) and a line of console output beside a print file.
if [ "$t" = "WT001A" ] && [ -n "${WITNESS_FILES:-}" ]; then
    IFS="|"; for kv in $WITNESS_FILES; do printf '%b' "${kv#*=}" > "./${kv%%=*}"; done
fi
if [ "$t" = "WT001A" ] && [ -n "${WITNESS_STDOUT:-}" ]; then printf '%s\n' "$WITNESS_STDOUT"; fi
if [ -n "${WITNESS_STDERR:-}" ]; then echo "$WITNESS_STDERR" >&2; fi
exit "${WITNESS_RC:-0}"
SHIM
    chmod +x "$BIN/dotnet"

    # The `diff` shim — only for the case that forces `diff` to fail. Everything else runs the real diff.
    local REAL_DIFF; REAL_DIFF="$(command -v diff)"
    { echo '#!/bin/bash'
      echo 'if [ -n "${WITNESS_DIFF_RC:-}" ]; then echo "diff: witness-forced trouble" >&2; exit "$WITNESS_DIFF_RC"; fi'
      echo "exec $REAL_DIFF \"\$@\""
    } > "$BIN/diff"
    chmod +x "$BIN/diff"

    # run_case CASE RC STDERR DIFF_RC [DLL=yes|no] -> prints the group runner's verdict line for WT001A
    run_case() {
        local case_name="$1" wrc="$2" werr="$3" wdiff="$4" dll="${5:-yes}"
        rm -rf "$FR/tests/nist/output"; mkdir -p "$FR/tests/nist/output"
        if [ "$dll" = "yes" ]; then
            local prog; for prog in ${WITNESS_GROUP:-WT001A}; do printf 'stub\n' > "$FR/tests/nist/output/$prog.dll"; done
        fi
        GUARD_FORENSICS="$d/forensics" \
        PATH="$BIN:$PATH" \
        WITNESS_CASE="$case_name" WITNESS_RC="$wrc" WITNESS_STDERR="$werr" WITNESS_DIFF_RC="$wdiff" \
        WITNESS_VALID="$FR/tests/nist/valid" \
            bash "$GROUP_RUNNER" "$FR" gW "${WITNESS_GROUP:-WT001A}" 2>>"$d/stderr.txt"
    }
    # compile_case CRC CLOG_TEXT -> the compile arm, with no .dll at all
    compile_case() {
        rm -rf "$FR/tests/nist/output"; mkdir -p "$FR/tests/nist/output"
        if [ -n "$1" ]; then printf '%s\n' "$1" > "$FR/tests/nist/output/WT001A.compile.rc"; fi
        printf '%s' "$2" > "$FR/tests/nist/output/WT001A.compile.log"
        GUARD_FORENSICS="$d/forensics" PATH="$BIN:$PATH" \
            bash "$GROUP_RUNNER" "$FR" gW "WT001A" 2>>"$d/stderr.txt"
    }
    # want NAME EXPECTED-SUBSTRING ACTUAL-LINE
    want() {
        if printf '%s' "$3" | grep -qF -- "$2"; then
            echo "  ok: $1"
        else
            echo "  WITNESS FAILED: $1"
            echo "      expected a verdict containing: $2"
            echo "      got:                           $3"
            rc=1
        fi
    }

    echo "=== (1) evidence-rule witnesses (through $GROUP_RUNNER) ==="

    # ── THE CONTROLS. Without these the fix could turn EVERY outcome into a NO-VERDICT and look green. ──
    want "control: a correct report is a MATCH" \
         "WT001A: MATCH" "$(run_case correct 0 '' '')"
    want "control: a correct report on STDOUT is a MATCH" \
         "WT001A: MATCH" "$(run_case stdout 0 '' '')"
    # ⭐ THE DISCRIMINATOR: a genuinely wrong answer must still be a REGRESSION.
    want "discriminator: a genuinely wrong report is a REGRESSION" \
         "WT001A: DIFF — REGRESSION!" "$(run_case wrong 0 '' '')"
    want "discriminator: a wrong report with a REASON is a REGRESSION" \
         "DIFF — REGRESSION! (run exited 1: boom)" "$(run_case wrong 1 boom '')"

    # ── THE COMPARE ARM (kb/Work/PB473). A comparison that could not be made is not a difference. ──
    want "a report that never appeared is NOT a regression" \
         "RUN NO-VERDICT (produced no report" "$(run_case absent 0 '' '')"
    want "a 0-byte report is NOT a regression" \
         "RUN NO-VERDICT (produced no report" "$(run_case empty 0 '' '')"
    want "diff itself failing (exit 2) is NOT a regression" \
         "COMPARE NO-VERDICT" "$(run_case correct 0 '' 2)"
    want "diff failing on a WRONG report is also NOT a regression" \
         "COMPARE NO-VERDICT" "$(run_case wrong 0 '' 2)"

    # ── THE RUN ARM's other half: a failure with no reason is a lost result, exactly as on the compile arm. ──
    want "rc!=0 with an EMPTY stderr is NOT a regression" \
         "RUN NO-VERDICT (rc=1 with NO diagnostic" "$(run_case wrong 1 '' '')"
    want "a timeout is NOT a regression" \
         "RUN NO-VERDICT (timeout/kill" "$(run_case wrong 124 '' '')"

    # ── THE FOOTER/FAIL* RULES (the IX108A false green) — they survive the extraction. ──
    printf 'FEATURE 1                PASS\nFEATURE 2                FAIL*\n NO TEST(S) FAILED\n' \
        > "$FR/tests/nist/valid/WT001A.txt"
    want "a matching report with FAIL* lines is a MATCH that SAYS SO, and names what the golden allows" \
         "WT001A: MATCH (1 FAIL*, golden allows 0)" "$(run_case correct 0 '' '')"
    printf 'FEATURE 1                PASS\n 001 TEST(S) FAILED\n' > "$FR/tests/nist/valid/WT001A.txt"
    # ⛔ THE ALLOWANCE COMES FROM THE GOLDEN (kb/Work PB436). This arm is reached only after a candidate
    # matched the golden, so comparing its footer to the CONSTANT 0 was auditing the BASELINE and not the run,
    # and it turned a CONFORMING compiler red on NC201A. What stops a FAILING report from being baselined as
    # the golden — the IX108A false green — is the baseline audit below, which is now the only place that
    # question is asked.
    want "a nonzero footer the GOLDEN itself carries is a MATCH, not a regression" \
         "WT001A: MATCH" "$(run_case correct 0 '' '')"
    printf 'FEATURE 1                PASS\nFEATURE 2                PASS\n NO TEST(S) FAILED\n' \
        > "$FR/tests/nist/valid/WT001A.txt"

    # ── THE WRITTEN-FILES ARM (kb/Work PB804). The report is ONE file of the run; every other file the program
    #    WRITES — created or modified since just before it ran — is compared byte-for-byte with
    #    tests/nist/valid-files/<TEST>/, and the two sets must be equal. Each case below is one way that can be wrong,
    #    and the first is the control that proves the arm does not turn every program red. NistWrittenFilesTests is
    #    the C# arm's twin of these cases (NistOutputs.Grade). ──
    local VF="$FR/tests/nist/valid-files/WT001A"
    gold() { rm -rf "$VF"; if [ "$#" -gt 0 ]; then mkdir -p "$VF"; for kv in "$@"; do printf '%b' "${kv#*=}" > "$VF/${kv%%=*}"; done; fi; }
    gold "xxxxx049.txt=DETAIL LINE  COLUMN 20"
    want "files control: a second file equal to its golden is a MATCH" \
         "WT001A: MATCH" "$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 20" run_case correct 0 '' '')"
    want "a second file with ONE byte different is a REGRESSION even though the report matches (the PB792 shape)" \
         "WT001A: DIFF — REGRESSION! (the report matched but 1 written file(s) do not match tests/nist/valid-files/WT001A: xxxxx049.txt differs" \
         "$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 21" run_case correct 0 '' '')"
    want "a second file that is a byte LONGER than its golden is a REGRESSION" \
         "xxxxx049.txt differs" "$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 20 " run_case correct 0 '' '')"
    want "a second file that is a byte SHORTER than its golden is a REGRESSION" \
         "xxxxx049.txt differs" "$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 2" run_case correct 0 '' '')"
    # The host newline: a line sequential file ends its records with CR LF on Windows and LF on Linux (DOC-A.1-114), so
    # in a TEXT file CR LF is not a difference (a lone CR is record data); in a BINARY file it is. The contents are
    # printf %b escapes (a CR cannot travel through the environment of this host's bash).
    gold 'xxxxx049.txt=LINE 1\nLINE 2\n'
    want "a TEXT file written with the host newline (CR LF) equals a golden written with LF" \
         "WT001A: MATCH" "$(WITNESS_FILES='xxxxx049.txt=LINE 1\r\nLINE 2\r\n' run_case correct 0 '' '')"
    want "a TEXT file whose lone CR the golden lacks is a REGRESSION (a lone CR is data)" \
         "xxxxx049.txt differs" "$(WITNESS_FILES='xxxxx049.txt=LINE 1\rLINE 2\n' run_case correct 0 '' '')"
    gold 'xxxxx049.txt=AB\002\nC'
    want "a BINARY file with a translated newline (LF written as CR LF) is a REGRESSION" \
         "xxxxx049.txt differs" "$(WITNESS_FILES='xxxxx049.txt=AB\002\r\nC' run_case correct 0 '' '')"
    want "a BINARY file equal to its golden is a MATCH" \
         "WT001A: MATCH" "$(WITNESS_FILES='xxxxx049.txt=AB\002\nC' run_case correct 0 '' '')"
    gold "xxxxx049.txt=DETAIL LINE  COLUMN 20"
    want "a golden the program no longer writes is a REGRESSION" \
         "xxxxx049.txt not written" "$(run_case correct 0 '' '')"
    want "a file written with NO golden is a REGRESSION" \
         "tf021.txt has no golden" "$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 20|tf021.txt=AB" run_case correct 0 '' '')"
    rm -rf "$d/forensics"
    out="$(WITNESS_FILES="xxxxx049.txt=DETAIL LINE  COLUMN 21" run_case correct 0 '' '')"
    if grep -q "xxxxx049.txt: differs from its golden" "$d/forensics/WT001A/files.problems" 2>/dev/null; then
        echo "  ok: a written-files regression preserves the list of problems as evidence"
    else
        echo "  WITNESS FAILED: a written-files regression kept no files.problems evidence in $d/forensics"
        ls -R "$d/forensics" 2>&1 | sed 's/^/      /'; rc=1
    fi
    gold
    want "files control: no second file and no golden directory is a MATCH" \
         "WT001A: MATCH" "$(run_case correct 0 '' '')"
    want "a program that writes a file when nothing is expected is a REGRESSION (no golden directory at all)" \
         "tf021.txt has no golden" "$(WITNESS_FILES="tf021.txt=AB" run_case correct 0 '' '')"
    # The program's console output beside its print file is a written file too, named (stdout).
    gold "(stdout)=HELLO"
    want "stdout control: console output equal to its golden is a MATCH" \
         "WT001A: MATCH" "$(WITNESS_STDOUT="HELLO" run_case correct 0 '' '')"
    want "stdout is graded on the acceptance basis: trailing spaces are not a difference" \
         "WT001A: MATCH" "$(WITNESS_STDOUT="HELLO   " run_case correct 0 '' '')"
    want "console output of only blanks printed NOTHING: a golden for it is not written" \
         "(stdout) not written" "$(WITNESS_STDOUT="   " run_case correct 0 '' '')"
    want "extra trailing blank lines on the console are not a difference (the acceptance basis, as the C# arm grades it)" \
         "WT001A: MATCH" "$(WITNESS_STDOUT="HELLO

" run_case correct 0 '' '')"
    want "console output that differs from its golden is a REGRESSION" \
         "(stdout) differs" "$(WITNESS_STDOUT="HOLA" run_case correct 0 '' '')"
    want "console output with no golden is a REGRESSION" \
         "(stdout) has no golden" "$(gold; WITNESS_STDOUT="HELLO" run_case correct 0 '' '')"
    gold "(stdout)=HELLO"
    want "a (stdout) golden for a program that printed nothing is a REGRESSION" \
         "(stdout) not written" "$(run_case correct 0 '' '')"
    gold
    # A file an EARLIER program of the group wrote, and this one did not touch, is not this one's output: the
    # group (a producer/consumer chain) shares a directory, and the marker is taken just before each program runs.
    printf 'FEATURE 1                PASS\nFEATURE 2                PASS\n NO TEST(S) FAILED\n' \
        > "$FR/tests/nist/valid/WT002A.txt"
    gold "tf021.txt=AB"
    want "a chain member that leaves its predecessor's file alone is graded on its own writes only" \
         "WT002A: MATCH" \
         "$(WITNESS_GROUP="WT001A WT002A" WITNESS_FILES="tf021.txt=AB" run_case correct 0 '' '')"
    gold
    rm -f "$FR/tests/nist/valid/WT002A.txt"

    # ── THE WRITTEN-FILES ARM'S OWN FAILURE MODES (kb/Work PB804, branch review round 1). A grader that could not
    #    ENUMERATE or COMPARE the files observed nothing, so it must say FILES NO-VERDICT and never score the run on
    #    an empty written set (an empty set reads as a pass for a program that has no golden directory). Each arm is
    #    driven through guard_files_apply itself with the failing step made to fail: a missing marker or run
    #    directory, a `find` that exits non-zero, a canonicalization that fails (`sed`). The control is the same
    #    inputs with nothing failing, which proves the arm does not answer NO-VERDICT for everything. ──
    fa_case() {      # fa_case SHIM-COMMAND-OR-EMPTY MARKER-MODE(present|missing|nodir)  -> "CLASS|VERDICT"
        local shim="$1" mode="$2" fd="$d/fa" shimdir="$d/fa-shim"
        rm -rf "$fd" "$shimdir"; mkdir -p "$fd/run" "$fd/gold" "$shimdir"
        printf 'AB\n' > "$fd/run/xxxxx014.txt"; printf 'AB\n' > "$fd/gold/xxxxx014.txt"
        : > "$fd/marker"; touch -d '2000-01-01 00:00:00' "$fd/marker"       # the file above is newer than the marker
        : > "$fd/stdout"
        case "$mode" in missing) rm -f "$fd/marker" ;; nodir) rm -rf "$fd/run" ;; esac
        if [ -n "$shim" ]; then
            printf '#!/bin/bash\necho "%s: witness-forced failure" >&2\nexit 3\n' "$shim" > "$shimdir/$shim"
            chmod +x "$shimdir/$shim"
        fi
        (
            export PATH="$shimdir:$PATH"
            . "$ROOT/scripts/guard-verdict.sh"
            GUARD_CLASS="match"; GUARD_VERDICT="MATCH"
            guard_files_apply WT001A "$fd/gold" "$fd/run" "$fd/marker" "wt001a.txt" "$fd/stdout" "$fd/problems.txt" 2>/dev/null
            printf '%s|%s' "$GUARD_CLASS" "$GUARD_VERDICT"
        )
    }
    want "files control: the same inputs with nothing failing stay a MATCH" "match|MATCH" "$(fa_case '' present)"
    want "a run directory that is gone is FILES NO-VERDICT, never a pass" \
         "no-verdict|FILES NO-VERDICT (the run directory or its marker is gone" "$(fa_case '' nodir)"
    want "a marker that was never written is FILES NO-VERDICT, never a pass on an empty written set" \
         "no-verdict|FILES NO-VERDICT (the run directory or its marker is gone" "$(fa_case '' missing)"
    want "a find that fails enumerating the written files is FILES NO-VERDICT" \
         "no-verdict|FILES NO-VERDICT (enumerating the files the program wrote failed, rc=3)" "$(fa_case find present)"
    want "a canonicalization that fails is FILES NO-VERDICT, never a difference and never a pass" \
         "no-verdict|FILES NO-VERDICT (canonicalizing xxxxx014.txt for the comparison failed)" "$(fa_case sed present)"

    # ── THE COMPILE ARM — unchanged rules, re-witnessed because they now run from the shared library. ──
    want "no .dll and no exit status is a COMPILE NO-VERDICT" \
         "COMPILE NO-VERDICT (the compile never reported an exit status)" "$(compile_case '' '')"
    want "no .dll with rc=0 is a COMPILE NO-VERDICT" \
         "COMPILE NO-VERDICT (rc=0 but no .dll" "$(compile_case 0 '')"
    want "no .dll with rc!=0 and NO diagnostic is a COMPILE NO-VERDICT" \
         "COMPILE NO-VERDICT (rc=1 with NO diagnostic" "$(compile_case 1 '')"
    want "no .dll with rc!=0 AND a diagnostic is a COMPILE FAILURE" \
         "COMPILE FAILED — REGRESSION! (rc=1: syntax error at line 4)" "$(compile_case 1 'syntax error at line 4')"

    # ── THE TERMINATION ARM (kb/Work PB1955). A row corpus.tsv declares `TERMINATES EC-…` is scored on how its
    #    run unit ENDS. CI run 37217227958 scored PB322's two such rows as `RUN NO-VERDICT (produced no report)` and
    #    `DIFF — REGRESSION! (run exited 1: …EC-I-O-LOGIC-ERROR…)` — the two shapes the first two cases fix. ──
    local abend="abnormal run-unit termination: EC-I-O-LOGIC-ERROR (fatal): I-O status 47 on SQ-FS1 (READ)"
    want "a declared termination with no report (NC401M's CI shape) is TERMINATES" \
         "WT001A: TERMINATES EC-I-O-LOGIC-ERROR (run exited 1" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case absent 1 "$abend" '')"
    # ⛔ AND THE FILES THE RUN LEFT AT ITS ABNORMAL END ARE GRADED (kb/Work PB804, branch review round 1; the owner's
    #    PB3353 requirement that the abnormal paths are covered as fully as the success paths). No report is separated
    #    out: the print file is a written file like the rest, and the console output counts whenever it printed.
    local partial='FEATURE 1                FAIL\nFEATURE 2                FAIL\n NO TEST(S) FAILED\n'
    gold "wt001a.txt=$partial"
    want "a declared termination after a partial report (ST146A's CI shape) is TERMINATES" \
         "WT001A: TERMINATES EC-I-O-LOGIC-ERROR" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case wrong 1 "$abend" '')"
    gold "wt001a.txt=${partial}X"
    want "a declared termination whose partial print file differs from its golden is a REGRESSION" \
         "DIFF — REGRESSION! (the run ended as declared but 1 written file(s) do not match tests/nist/valid-files/WT001A: wt001a.txt differs" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case wrong 1 "$abend" '')"
    gold
    want "a declared termination that left a print file with NO golden is a REGRESSION" \
         "wt001a.txt has no golden" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case wrong 1 "$abend" '')"
    gold "xxxxx014.txt=AB"
    want "a declared termination whose closed data file equals its golden is TERMINATES" \
         "WT001A: TERMINATES EC-I-O-LOGIC-ERROR" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" WITNESS_FILES="xxxxx014.txt=AB" run_case absent 1 "$abend" '')"
    want "a declared termination whose closed data file lost a record is a REGRESSION (ST146A's SQ-FS1 shape)" \
         "xxxxx014.txt differs" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" WITNESS_FILES="xxxxx014.txt=A" run_case absent 1 "$abend" '')"
    want "a declared termination that never wrote the data file its golden holds is a REGRESSION" \
         "xxxxx014.txt not written" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case absent 1 "$abend" '')"
    gold "(stdout)=HELLO"
    want "a declared termination whose console output equals its golden is TERMINATES" \
         "WT001A: TERMINATES EC-I-O-LOGIC-ERROR" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" WITNESS_STDOUT="HELLO  " run_case absent 1 "$abend" '')"
    want "a declared termination whose console output differs from its golden is a REGRESSION" \
         "(stdout) differs" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" WITNESS_STDOUT="HOLA" run_case absent 1 "$abend" '')"
    gold
    want "a declared termination whose console output has no golden is a REGRESSION (no print file is needed for it to count)" \
         "(stdout) has no golden" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" WITNESS_STDOUT="HELLO" run_case absent 1 "$abend" '')"
    gold "wt001a.txt=$partial"
    want "a declared termination that ran to a normal end is a REGRESSION, even with the golden's report" \
         "DIFF — REGRESSION! (ran to a normal end; corpus.tsv declares it TERMINATES on EC-I-O-LOGIC-ERROR)" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case correct 0 '' '')"
    want "ending on a DIFFERENT exception is a REGRESSION" \
         "but not on the declared EC-I-O-PERMANENT-ERROR" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-PERMANENT-ERROR" run_case absent 1 "$abend" '')"
    want "the declared name matches WHOLE: EC-I-O is not satisfied by EC-I-O-LOGIC-ERROR" \
         "but not on the declared EC-I-O:" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O" run_case absent 1 "$abend" '')"
    want "a declared termination with an EMPTY stderr is NOT scored" \
         "RUN NO-VERDICT (rc=1 with NO diagnostic" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case absent 1 '' '')"
    want "a declared termination that timed out is NOT scored" \
         "RUN NO-VERDICT (timeout/kill" \
         "$(GUARD_TERMINATES="WT001A=EC-I-O-LOGIC-ERROR" run_case absent 124 "$abend" '')"
    want "another program's declaration does not touch this one" \
         "WT001A: MATCH" \
         "$(GUARD_TERMINATES="WT002A=EC-I-O-LOGIC-ERROR" run_case correct 0 '' '')"
    gold

    # ── THE EVIDENCE IS KEPT (PB473 item 4): a non-MATCH's work dir dies with its group. ──
    rm -rf "$d/forensics"
    out="$(run_case wrong 0 '' '')"
    if [ -f "$d/forensics/WT001A/wt001a.txt" ] && [ -f "$d/forensics/WT001A/VERDICT.txt" ] \
       && [ -f "$d/forensics/WT001A/expected.norm" ] && [ -f "$d/forensics/WT001A/actual.norm" ]; then
        echo "  ok: a non-MATCH preserves its report, its streams and both normalized sides"
    else
        echo "  WITNESS FAILED: a non-MATCH preserved no evidence in $d/forensics"
        ls -R "$d/forensics" 2>&1 | sed 's/^/      /'; rc=1
    fi
    rm -rf "$d/forensics"
    out="$(run_case correct 0 '' '')"
    if [ -d "$d/forensics" ]; then
        echo "  WITNESS FAILED: a MATCH wrote forensics — a green run must cost nothing"; rc=1
    else
        echo "  ok: a MATCH preserves nothing"
    fi

    # ── THE BASELINE AUDIT (scripts/guard-baselines.sh) — the ONE copy guard.sh and guard-fast.sh both call.
    # Every arm is fired here, because an allowance nobody has seen refuse is not an allowance, it is a hole.
    local BV="$d/bl"
    mkdir -p "$BV/valid"
    printf 'FEATURE 1                PASS\n NO TEST(S) FAILED\n'   > "$BV/valid/BL001A.txt"
    printf 'FEATURE 1                FAIL*\n 001 TEST(S) FAILED\n' > "$BV/valid/BL002A.txt"
    printf '#name\tsuite\tstatus\tpreds\tgolden\tnote\n' > "$BV/none.tsv"
    printf '#name\tsuite\tstatus\tpreds\tgolden\tnote\nBL002A\tNC\tdivergent\t-\tvalid\tISO 1.1 CCVS-DEFECT: declared\n' > "$BV/ok.tsv"
    printf '#name\tsuite\tstatus\tpreds\tgolden\tnote\nBL002A\tNC\tgreen\t-\tvalid\tCCVS-DEFECT: uncited\n' > "$BV/green.tsv"
    printf '#name\tsuite\tstatus\tpreds\tgolden\tnote\nBL001A\tNC\tdivergent\t-\tvalid\tISO 1.1 CCVS-DEFECT: stale\nBL002A\tNC\tdivergent\t-\tvalid\tISO 1.1 CCVS-DEFECT: declared\n' > "$BV/stale.tsv"
    want "an UNDECLARED baseline carrying a FAIL* is refused" \
         "BL002A.txt contains 1 FAIL*" "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/none.tsv" 2>&1)"
    want "an UNDECLARED baseline carrying a nonzero footer is refused (the IX108A false green)" \
         "BL002A.txt footer reports 001 TEST(S) FAILED" "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/none.tsv" 2>&1)"
    want "a DECLARED CCVS-DEFECT baseline is allowed, and says so" \
         "DECLARED CCVS-DEFECT" "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/ok.tsv" 2>&1)"
    want "a CCVS-DEFECT declaration that is not \`divergent\` is refused (its ISO citation would be unenforced)" \
         "status is not" "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/green.tsv" 2>&1)"
    want "a STALE declaration (declared, but the baseline carries nothing) is refused" \
         "BL001A is declared CCVS-DEFECT but its baseline carries no failure" \
         "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/stale.tsv" 2>&1)"
    : > "$BV/valid/BL003A.txt"
    want "a 0-byte baseline is refused (it would match a crashed run vacuously)" \
         "BL003A.txt is EMPTY" "$(bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/ok.tsv" 2>&1)"
    rm -f "$BV/valid/BL003A.txt"
    if bash "$ROOT/scripts/guard-baselines.sh" "$BV/valid" "$BV/ok.tsv" >/dev/null 2>&1; then
        echo "  ok: the audit EXITS 0 when every baseline is clean or declared"
    else
        echo "  WITNESS FAILED: the baseline audit refuses a clean tree (scripts/guard-baselines.sh)"; rc=1
    fi

    # ── STRUCTURAL: the rule stays in ONE place, and the compare never reads through a pipe it cannot check. ──
    # (Comment lines are exempt — this file and guard-verdict.sh both QUOTE the old construct to explain it.)
    awk '$0 !~ /^[ \t]*#/ && ($0 ~ /diff[ \t]*<\(/ || $0 ~ /comm[ \t].*<\(/) { print FILENAME ":" FNR ": " $0 }' \
        scripts/guard.sh scripts/guard-run-group.sh scripts/guard-fast.sh scripts/guard-verdict.sh \
        scripts/guard-baselines.sh \
        > "$d/procsub.txt" 2>/dev/null
    if [ -s "$d/procsub.txt" ]; then
        echo "  WITNESS FAILED: a verdict-bearing comparison reads through a PROCESS SUBSTITUTION"
        echo "      (a short delivery over /dev/fd/N is indistinguishable from a difference — kb/Work/PB473)"
        sed 's/^/      /' "$d/procsub.txt"; rc=1
    else
        echo "  ok: no verdict-bearing comparison reads through a process substitution"
    fi
    for f in scripts/guard.sh scripts/guard-run-group.sh; do
        if grep -q 'guard_output_verdict' "$f" && grep -q 'guard_compile_verdict' "$f" \
           && grep -q 'guard_termination_verdict' "$f" && ! grep -q '^ *normalize()' "$f"; then
            echo "  ok: $f scores through the shared evidence rules and defines none of its own"
        else
            echo "  WITNESS FAILED: $f has grown its own copy of the verdict rules (feedback_one_rule_one_place)"
            rc=1
        fi
    done

    if [ "$rc" -eq 0 ]; then
        echo "=== (1) WITNESSES: ALL GREEN — every evidence rule proved on the real group runner ==="
    else
        echo "=== (1) WITNESSES: FAILED — the harness's verdicts do not mean what they say ==="
        [ -s "$d/stderr.txt" ] && { echo "--- group-runner stderr ---"; sed 's/^/    /' "$d/stderr.txt"; }
    fi
    return $rc
}

W_RC=0
# ⛔ WHICH COMPILER THE GUARD DRIVES IS PART OF WHAT ITS VERDICTS MEAN (kb/Work/PB750), so the identity
# watchdog is proven able to REFUSE in the same seconds-long phase as the evidence rules. Both are instruments;
# neither is believed until it has been shown to fail. Its own output carries the ALL GREEN / FAILED line the
# battery greps.
bash "$ROOT/scripts/guard-compiler.sh" --self-test || W_RC=1
witnesses || W_RC=1
if [ "$MODE" = "witnesses" ]; then
    exit $W_RC
fi
if [ "$W_RC" -ne 0 ]; then
    echo "=== SKIPPING the equivalence proof: two guards agreeing about a rule that is WRONG is not evidence ==="
    exit 1
fi

# ── (2) THE EQUIVALENCE PROOF ──────────────────────────────────────────────────────────────────────────────
# This run's OWN scratch directory (kb/Work PB2922): fixed names (gv_serial.log, ...) directly in the machine-wide temp
# directory were one file for every run on the host. Removed when the proof is GREEN; kept, and named, when it is not.
TMP="$(mktemp -d "${TMPDIR:-/tmp}/guard-verify.XXXXXX")"
echo "=== serial scripts/guard.sh ==="
bash scripts/guard.sh > "$TMP/gv_serial.log" 2>&1; SER=$?
echo "=== parallel scripts/guard-fast.sh ==="
bash scripts/guard-fast.sh > "$TMP/gv_fast.log" 2>&1; FAST=$?

# Normalize verdict lines (strip indent, keep the FAIL* count) and sort so order (which differs by design) is moot.
# ⛔ THE VOCABULARY MUST BE COMPLETE. An earlier pattern omitted one verdict word, so 11 programs were silently
# dropped from BOTH sides and the equivalence proof never compared them — the same defect class this whole wave
# closes (a filter that quietly excludes reads as agreement). Any line that looks like a verdict but matches no
# known word is surfaced by the UNKNOWN check below rather than discarded.
VERDICT_WORDS="MATCH|DIFF|FOOTER|COMPILE FAILED|COMPILE NO-VERDICT|RUN NO-VERDICT|COMPARE NO-VERDICT|NO BASELINE|TERMINATES"
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
if [ "$RC" -eq 0 ]; then rm -rf "$TMP"; else echo "=== both guards' logs and verdict lists: $TMP ==="; fi
exit $RC
