#!/bin/bash
# ⛔ THE ONE PYTHON INTERPRETER the gate scripts run — sourced, never executed: `. "$(dirname "$0")/python-resolve.sh"`,
# then `"$PY" script.py …`. A script with a verdict line of its own sources it in a subshell, so that a host with no
# interpreter still ends on that line: `PY="$(. scripts/python-resolve.sh && printf '%s' "$PY")" || <its NOT RUN>`
# (build-local.sh, linux-gate.sh). Its PowerShell twin, scripts/python-resolve.ps1, probes the same candidates against
# the same floor for build-local.ps1, and PythonInterpreterDriftTests holds every gate script on one of the two
# (kb/Work PB3197).
#
# WHY THIS EXISTS (kb/Work PB1637). The gates spelled the interpreter `python3`. On the Windows dev box that name is
# the Microsoft Store APP-EXECUTION ALIAS once the real CPython install sits on PATH only as `python` (the 2026-09-24
# CPython 3.14 PATH change): it EXISTS, so `command -v python3` finds it, and it RUNS, printing "Python was not
# found" and exiting non-zero. Battery #87's phase −1 therefore ran none of its four citation audits and reported four
# ⛔ lines whose cause was not the tree, and guard-fast's leg reporter would have turned green legs red for the same
# reason. A name on PATH is not an interpreter, so each candidate is PROBED by running it.
#
# Candidates in order: an explicit $PYTHON (the override), `python3` (Linux CI, WSL, macOS), `python` (Windows).
# The probe accepts EXACTLY the interpreter toolchain.env names (PYTHON_VERSION; kb/Work PB4007): CI and every local
# host run the same Python, so an interpreter of another version is not this repository's, however new. (It was a
# floor, 3.12, while CI's Windows jobs ran the runner image's 3.12 and every local host 3.14, and a self-test correct
# on every local leg went red in a merge group.)
# Nothing runnable ⇒ exit non-zero, loudly: a gate that cannot run its checks is RED, never silently skipped.
__py_want="$(sed -n 's/^PYTHON_VERSION=\([0-9.]*\)$/\1/p' "$(dirname "${BASH_SOURCE[0]}")/../toolchain.env" 2>/dev/null)"
PY=""
for __py_cand in ${PYTHON:+"$PYTHON"} python3 python; do
    if [ -n "$__py_want" ] && "$__py_cand" -c 'import platform, sys; sys.exit(0 if platform.python_version() == sys.argv[1] else 1)' "$__py_want" >/dev/null 2>&1; then
        PY="$__py_cand"
        break
    fi
done
unset __py_cand
if [ -z "$PY" ]; then
    echo "python-resolve: ⛔ no runnable Python ${__py_want:-(toolchain.env names no PYTHON_VERSION)} (toolchain.env; tried \$PYTHON, python3, python) — the gate cannot run: install it (winget on the Windows dev box, scripts/wsl/setup-wsl.sh in WSL)" >&2
    unset __py_want
    exit 1
fi
unset __py_want
export PY
