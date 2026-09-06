#!/bin/bash
# Compile ONE NIST program with the guard's COMPILER UNDER TEST, keeping the diagnostics AND the exit status.
#
#   bash scripts/guard-compile.sh <TEST> <SOURCE.cob> <OUT-DIR>
#
# Writes <OUT-DIR>/<TEST>.dll (on success), <OUT-DIR>/<TEST>.compile.log (always) and
# <OUT-DIR>/<TEST>.compile.rc (always). Never fails the caller: the COMPILE evidence rule in
# scripts/guard-verdict.sh scores those three artefacts, and "no .dll" is not a verdict on its own.
#
# ⛔ WHY THIS IS ITS OWN FILE. The two compilers spell the NIST switch differently — `cobol` takes the test name
# as `--nist NAME` (its parser binds the next token to the option, so `--nist prog.cob` consumes the SOURCE and
# the compile fails with "Required argument missing"), while the legacy `cobolsharp` takes a bare `--nist` and
# derives the name from the file. That difference is one rule about how to invoke a compiler, so it is written
# down ONCE here rather than twice inline in guard.sh and three times in guard-fast.sh — where the parallel
# compile, the serial re-observation retry and the serial guard would each have had to be kept in step by hand
# (`feedback_one_rule_one_place`; kb/Work/PB750).
#
# The caller exports GUARD_COMPILER and GUARD_CLI_DLL (scripts/guard-compiler.sh). guard.sh deliberately points
# GUARD_CLI_DLL at its run-scoped SNAPSHOT of the CLI, so this script must never re-resolve them itself.
set -u

TEST="${1:?usage: guard-compile.sh TEST SOURCE OUTDIR}"
SRC="${2:?usage: guard-compile.sh TEST SOURCE OUTDIR}"
OUTDIR="${3:?usage: guard-compile.sh TEST SOURCE OUTDIR}"

CLI="${GUARD_CLI_DLL:-}"
COMPILER="${GUARD_COMPILER:-}"
if [ -z "$CLI" ] || [ -z "$COMPILER" ]; then
    printf 'guard-compile.sh: GUARD_CLI_DLL / GUARD_COMPILER not exported by the caller — refusing to guess\n' \
        > "$OUTDIR/$TEST.compile.log"
    echo "90" > "$OUTDIR/$TEST.compile.rc"
    exit 0
fi

rc=0
if [ "$COMPILER" = "legacy" ]; then
    dotnet "$CLI" --nist "$SRC" -o "$OUTDIR/$TEST.dll" > "$OUTDIR/$TEST.compile.log" 2>&1 || rc=$?
else
    # `--nist NAME` is explicit rather than derived: the CCVS X-card substitution keys on the test name, and an
    # explicit name is what makes the invocation independent of the source file's basename (guard.sh compiles
    # from a run-scoped snapshot directory).
    dotnet "$CLI" --nist "$TEST" "$SRC" -o "$OUTDIR/$TEST.dll" > "$OUTDIR/$TEST.compile.log" 2>&1 || rc=$?
fi
echo "$rc" > "$OUTDIR/$TEST.compile.rc"
exit 0
