#!/usr/bin/env bash
# build-local.sh — THE GATE, as one command (kb/Work PB1708, PB3022, PB4328; docs/rearchitecture/DESIGN-test-build-ci.md
# §3.14.1–3.14.6, §3.17.3; the bash twin of build-local.ps1). It hands the gate to scripts/gate.py, which holds this
# worktree's gate lock, decides the change's lane, runs the audits beside everything after them, takes a gate slot,
# builds the solution and runs the WHOLE Unit, Conformance and Characterization populations, three hosts at once (the
# classes red at the previous gate first, then longest first by the committed weights), checks each population by name
# and prints `=== BUILD-LOCAL GATE: … ===`.
# ⛔ THE GATE OUTLIVES THIS COMMAND (kb/Work PB3536; DESIGN-test-build-ci.md §3.14.12): the driver starts the gate
# DETACHED in its run directory (`TestResults/build-local/<run>/`: gate.log, gate.pid, verdict.json) and follows it, so
# a caller killed mid-gate leaves it running to its verdict, which `python scripts/gate.py --await <run>`
# reads; and a re-gate of an unchanged tree answers from the recorded GREEN (`… GREEN — REUSED: …`), running nothing.
# ⛔ ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015/PB3022): there is no mode and no scope; CI's merge group is
# the first run of the merged tree, Linux and Release.
# ⛔ ORDER, DON'T SKIP (owner, 2026-09-28): no gate filters.
# Usage:  bash scripts/build-local.sh
set -u
if [ "$#" -ne 0 ]; then
    echo "usage: $0 — the gate takes no arguments: it has one shape (kb/Work PB3022)" >&2
    exit 2
fi
cd "$(dirname "$0")/.."
# THE ONE INTERPRETER (scripts/python-resolve.sh; kb/Work PB1637, PB3197): a bare `python` died `command not found`
# (exit 127, no verdict line) on a host that has only `python3`. Resolved in a subshell, so a host with no runnable
# Python ends on the gate's own verdict line, NOT RUN [interpreter] (ENVIRONMENT, gate.NOT_RUN_CAUSES) with
# exit 2 as the driver's NOT RUN does, the reason above it.
if ! PY="$(. scripts/python-resolve.sh && printf '%s' "$PY")"; then
    echo "=== BUILD-LOCAL GATE: NOT RUN [interpreter] — python-resolve.sh found no runnable Python (its reason is printed above) ==="
    exit 2
fi
"$PY" scripts/gate.py
