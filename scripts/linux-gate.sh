#!/usr/bin/env bash
# linux-gate.sh — run CI's LINUX test legs locally, under WSL, before a push spends a CI run on them.
#
#   From any shell, at the tree's root:  bash scripts/linux-gate.sh [--legs unit,characterization,conformance,guard] [--nice]
#   On a host that is not Linux (Git Bash, PowerShell's bash) it runs ITSELF under WSL, in the same tree with the same
#   arguments, and its verdict is that run's (run_on_linux, kb/Work PB2999).
#   python scripts/self_tests.py runs `bash scripts/linux-gate.sh --self-test` (the host hand-off, with stand-ins).
#
# ⛔ WHY IT EXISTS (kb/Work PB1732). The implementer and lander gates ran on Windows only, so CI's Linux jobs were the
# first Linux run a change ever got. On 2026-09-28 train 71b's first push went RED in `Greenfield unit +
# characterization (Linux)`: group F's new GateLegDriftTests.Arm5 planted the Windows root `E:\COBOL-wt\battery87`,
# which `Path.GetFullPath` treats as RELATIVE on Linux. The unit leg reproduces exactly that one red; the CI round
# trip cost ~30 min and a dropped cluster. MANDATORY-PRACTICES I8 (implementers) and L10 (landers) require this
# script, check_practices.py refuses a brief without it, and LinuxGateDriftTests keeps its legs equal to what CI's
# Linux jobs run: every test project they `dotnet test`, and every repository script they `bash` (the `guard` leg,
# CI's guard job, added 2026-10-04 by kb/Work PB1955 after that job's red reached CI unseen).
#
# HOW: a LINUX CLONE OF THE COMMIT, never the Windows tree. The tree's committed HEAD is cloned (`--shared`: it
# borrows the Windows repository's object store read-only, so it copies nothing) into ~/linux-gate/<tree>.<pid>/<tree>
# on the Linux filesystem, built there with the Linux SDK, and tested there — the shape of CI's ubuntu jobs. The gate
# removes that directory when it ends, on every exit path, and reaps what killed gates left (kb/Work PB3194, below).
# Two earlier designs failed, each measured:
#  1. Running the Windows-built binaries: the Conformance tests find their goldens through [CallerFilePath], which a
#     Windows build bakes in as `E:\…` paths, relative on Linux — 363 false reds.
#  2. Running in the Windows tree with GIT_DIR/GIT_WORK_TREE exported so Linux git could read a Windows worktree
#     (whose `.git` file names an `E:/` gitdir): the variables reached EVERY test process, and a test that builds its
#     own scratch repository (gate_slot.py's self-test: git init, commit, worktree add) wrote into the REAL
#     repository instead — `core.worktree` landed in the shared E:\COBOL\.git\config and broke git for every
#     checkout until the owner removed it (2026-09-29, wave 72). So NOTHING here is exported: the only git calls on
#     the Windows repository are this script's own READS (`wgit`: HEAD, the git directory, the uncommitted-change
#     count, the tripwire), by the tree's own git and without optional locks (kb/Work PB2880), and the tests run in a
#     clone that is an ordinary Linux repository.
# The clone builds COMMITTED HEAD: commit before running it. Uncommitted tracked changes are counted and reported.
#
# THE INERT LANE AND ITS PROOF (kb/Work PB2878; DESIGN-test-build-ci.md §3.18). A HEAD whose change against origin/main
# touches only paths no test reads (scripts/inert_lane.py classify, THE one classification: CI's `changes` job and the
# Windows gate use it too) runs no leg: `=== LINUX GATE: GREEN (inert lane, …) ===`. Every other run watches every inert
# path with inotify while the test legs run (`inert_lane.py watch`), so a test that starts reading one is a RED here
# (the `inert-watch` leg) before CI's Linux steps, which run under the same watch, see it. The watched clone runs the
# three `dotnet test` legs and nothing else: the self-tests (audits, which read inert paths by design: the register, the
# DEVLOG, the skills submodule they initialize) and the guard run in a second clone with its own build, so no read of
# theirs reaches the watch.
#
# Output: TestResults/linux-gate/<leg>.log (+ .trx, + <leg>.observations.log: the test harness's retries and
# non-observations, copied in as the gate ends) and build logs in the tree, one `leg <name>: GREEN|RED|NOT RUN`
# line each, and ONE verdict:
#   === LINUX GATE: GREEN (legs …) ===   |   === LINUX GATE: RED (…) ===   |   === LINUX GATE: NOT RUN [<cause>] (…) ===
# Exit 0 only on GREEN. A NOT RUN names its cause by tag, one row of run_gate_legs.NOT_RUN_CAUSES, which classifies it
# ONCE (ENVIRONMENT or BRANCH: kb/Work PB3526); the driver's self-test holds every NOT RUN line here to a known tag.
set -u

# ⛔ THE GATE PUTS ITSELF ON ITS HOST (kb/Work PB2999; owner 2026-10-11: "Why not simply require it to invoke using the
# proper host rather than an after the fact check that the correct host was invoked"). Started from Git Bash — the Bash
# tool every session has, and the bare command the gate skill gives — this script ran on WINDOWS: it cloned into the
# Windows home, built with the Windows SDK and printed `LINUX GATE: RED (... build ...)`, a verdict about no Linux leg at
# all. So on a host that is not Linux it runs itself under WSL (`-d Ubuntu`, the distribution scripts/wsl/ provisions),
# in this tree with these arguments, and exits with that run's status; the one refusal left is a host with no WSL.
# MSYS_NO_PATHCONV: Git Bash rewrites an argument that looks like a POSIX path before a Windows program sees it
# (`/mnt/e/...` became `C:/Program Files/Git/mnt/e/...`, DEVLOG), and the hand-off passes the arguments verbatim.
# `--exec`, not `--`: after `--`, wsl.exe hands the command line to the distribution's default shell, which parses it
# again — measured 2026-10-11, `-- bash -lc '... "$@"' linux-gate --legs unit --nice` reached bash as `[/bin/bash][]`,
# every argument lost; `--exec` gives bash the arguments as they are: `[linux-gate][--legs][unit][--nice]`.
run_on_linux() {  # the script's own arguments
  local host here
  host="$(uname -s)"
  [ "$host" = "Linux" ] && return 0
  if ! command -v wsl.exe >/dev/null 2>&1; then
    echo "=== LINUX GATE: NOT RUN [host] (this host is $host, not Linux, and has no WSL to run the gate on) ==="; exit 2
  fi
  here="$(cygpath -w "$(pwd)" 2>/dev/null || pwd)"
  echo "linux-gate: this host is $host, not Linux: running the gate under WSL (Ubuntu) in $here"
  MSYS_NO_PATHCONV=1 wsl.exe -d Ubuntu --cd "$here" --exec bash -lc 'bash scripts/linux-gate.sh "$@"' linux-gate "$@"
  exit $?
}

# Every --legs name ($legs) is one of the selectable legs, into `wanted`, checked before anything is cloned or built: an
# unknown name used to reach the leg dispatch, where `selftests` (which always runs) wrote "unknown leg" over the real
# leg's result file and was reported twice (2026-10-11, PB2999's first live run).
check_legs() {
  local leg
  IFS=',' read -r -a wanted <<< "$legs"
  for leg in "${wanted[@]}"; do
    case "$leg" in
      unit|characterization|conformance|guard) ;;
      *) echo "linux-gate: unknown leg '$leg' (the selectable legs are unit, characterization, conformance and guard;" \
              "selftests always runs, and selftests-built with unit or conformance)"
         echo "=== LINUX GATE: NOT RUN [arguments] (bad arguments) ==="; exit 2 ;;
    esac
  done
}

# PY: THE ONE INTERPRETER the gate scripts run (scripts/python-resolve.sh; kb/Work PB1637, PB3197), or the gate is NOT
# RUN. This script took `command -v python3 || command -v python`, which accepts any name on PATH: a python3 below the
# floor the scripts are written for (3.12) was taken, and the legs then failed on their syntax instead of refusing. The
# resolver runs in a subshell, so a host with none ends on this gate's verdict line (exit 2, as every NOT RUN does).
resolve_py() {
  if ! PY="$(. "$(dirname "$0")/python-resolve.sh" && printf '%s' "$PY")"; then
    echo "=== LINUX GATE: NOT RUN [interpreter] (python-resolve.sh found no runnable Python here — run scripts/wsl/setup-wsl.sh once) ==="
    exit 2
  fi
}

# ⛔ A GATE LEAVES NOTHING BEHIND ON THE LINUX FILESYSTEM (kb/Work PB3194). Every gate cloned into ~/linux-gate/<tree>
# (about 1 GB and 80,000 inodes once built) plus a <tree>.guard-tmp beside it, and removed them only when the SAME tree
# was gated again. Worktrees come and go, so the clones accumulated: 345 of them filled the WSL disk on 2026-10-15
# (`No space left on device` mid-leg read as 2,919 conformance failures, then the distro remounted read-only), 430
# exhausted every inode on 2026-10-19 and 394 again on 2026-10-22, each time ending every Linux gate on the host until
# someone deleted clones by hand. So ONE mechanism owns the gate's footprint:
#  - each gate works in its OWN directory, GATES_ROOT/<tree>.<pid>, holding the clone (<tree>), the guard leg's
#    scratch (guard-tmp, beside the clone, never inside it: kb/Work PB2989), each test leg's harness observation log
#    (test_leg) and an `owner` file naming the gate's process (pid and start time, so a reused pid is not mistaken
#    for it);
#  - the gate removes that directory when it ends, on EVERY exit path: its verdict (GREEN, RED, NOT RUN after the
#    clone) through the EXIT trap, and a signal through on_signal, which first ends the legs' process trees. What the
#    gate keeps is what it wrote to the tree's TestResults/linux-gate (every log, trx and the guard's forensics,
#    guard_leg) and the observation logs, which remove_gate copies there first;
#  - a gate killed outright (SIGKILL, or WSL ending the session) runs no trap, so every gate first reaps
#    (reap_gates): a directory whose owner is no longer running is removed at once, and one with no owner file (the
#    layout before this mechanism, from a branch not yet merged with it) when it is this tree's or older than
#    LEGACY_MAX_AGE_S;
#  - then it measures the filesystem (check_disk) and refuses, NOT RUN [disk], below MIN_FREE_INODES free inodes or
#    MIN_FREE_KB free space: a full disk is said before any leg, never discovered as a red inside one.
# The bounds: one built gate is ~80,000 inodes and ~1 GB (measured 2026-10-23); the bounds hold about twelve of them,
# the gate cap (3, kb/Work PB2514) plus every implementer gate the host runs beside it.
GATES_ROOT="$HOME/linux-gate"
LEGACY_MAX_AGE_S=$((3 * 3600))
MIN_FREE_INODES=1000000
MIN_FREE_KB=$((20 * 1024 * 1024))

# /proc/<pid>/stat, read past the command name (which may hold spaces and parentheses): its fields after the LAST ')'
# are fixed. proc_field <pid> <n> prints field n of those (1 state, 2 parent pid, 20 start time), nothing if no process.
proc_field() {
  local line rest
  { read -r line < "/proc/$1/stat"; } 2>/dev/null || return 1
  rest="${line##*) }"
  set -- $2 $rest
  shift "$1"
  printf '%s' "$1"
}
# The gate in <dir> is still running: its owner file names a pid whose process started when the gate's did.
gate_alive() {
  local pid start
  read -r pid start _ < "$1/owner" 2>/dev/null || return 1
  [ -n "$start" ] && [ "$(proc_field "$pid" 20)" = "$start" ]
}
# reap_gates <root> <tree name>: remove every leftover of a gate that has ended (above), and say what went.
reap_gates() {
  local root="$1" self="$2" d name pid start why now gone=0 names=""
  now="$(date +%s)"
  for d in "$root"/*; do
    [ -e "$d" ] || continue
    name="${d##*/}"
    pid=""; start=""
    [ -f "$d/owner" ] && read -r pid start _ < "$d/owner" 2>/dev/null
    if [ -n "$pid" ] && [ -n "$start" ]; then
      gate_alive "$d" && continue
      why="its gate (pid $pid) has ended"
    elif [ "$name" = "$self" ] || [ "$name" = "$self.guard-tmp" ]; then
      why="this tree's earlier gate"
    elif [ $(( now - $(stat -c %Y "$d" 2>/dev/null || echo "$now") )) -gt "$LEGACY_MAX_AGE_S" ]; then
      why="older than $((LEGACY_MAX_AGE_S / 3600)) h"
    else
      continue
    fi
    if rm -rf "$d"; then
      gone=$((gone + 1)); names="$names $name"
    else
      echo "linux-gate: could not remove $d ($why)"
    fi
  done
  [ "$gone" -gt 0 ] && echo "linux-gate: removed $gone leftover gate director$([ "$gone" -eq 1 ] && echo y || echo ies) under $root:$names"
  return 0
}
# check_disk <dir>: NOT RUN [disk] when the filesystem holding <dir> has too few free inodes or too little free space.
# A filesystem that keeps no inode count (df -i says `-` or 0, as btrfs does) is held to the space bound alone.
check_disk() {
  local itotal ifree kfree
  read -r _ itotal _ ifree _ <<< "$(df -Pi "$1" 2>/dev/null | tail -1)"
  read -r _ _ _ kfree _ <<< "$(df -Pk "$1" 2>/dev/null | tail -1)"
  if ! [[ "$kfree" =~ ^[0-9]+$ ]]; then
    echo "=== LINUX GATE: NOT RUN [disk] (cannot measure the free space of the filesystem holding $1: df -Pk gave no number) ==="
    exit 2
  fi
  if [[ "$itotal" =~ ^[1-9][0-9]*$ ]] && [[ "$ifree" =~ ^[0-9]+$ ]] && [ "$ifree" -lt "$MIN_FREE_INODES" ]; then
    echo "=== LINUX GATE: NOT RUN [disk] ($ifree free inodes on the filesystem holding $1, below the bound" \
         "$MIN_FREE_INODES, after removing every ended gate's leftovers: free them, then gate again) ==="
    exit 2
  fi
  if [ "$kfree" -lt "$MIN_FREE_KB" ]; then
    echo "=== LINUX GATE: NOT RUN [disk] ($((kfree / 1024 / 1024)) GB free on the filesystem holding $1, below the" \
         "bound $((MIN_FREE_KB / 1024 / 1024)) GB, after removing every ended gate's leftovers: free it, then gate again) ==="
    exit 2
  fi
}
# begin_gate <root> <tree>: make this gate's own directory (gate, snap, gtmp) and arm its removal on every exit path.
begin_gate() {
  local name="${2##*/}" me="$BASHPID"   # captured here: inside $( ) BASHPID is that subshell's
  gate="$1/$name.$me"
  snap="$gate/$name"
  gtmp="$gate/guard-tmp"
  mkdir -p "$gate" && printf '%s %s %s\n' "$me" "$(proc_field "$me" 20)" "$2" > "$gate/owner"
  trap remove_gate EXIT
  trap 'on_signal 129' HUP
  trap 'on_signal 130' INT
  trap 'on_signal 143' TERM
}
remove_gate() {
  [ -n "${gate:-}" ] || return 0
  # The legs' observation logs go to the tree's results first (test_leg says why they are written here); `out` is the
  # tree's results directory, unset in the self-test.
  if [ -n "${out:-}" ] && compgen -G "$gate/*.observations.log" > /dev/null; then
    cp "$gate"/*.observations.log "$out/" || echo "linux-gate: could not copy the harness observation logs into $out"
  fi
  rm -rf "$gate" || echo "linux-gate: could not remove this gate's directory $gate; the next gate on this host reaps it"
  gate=""
}
# Every descendant of the pids given, by parent pid (field 2), read in ONE pass over /proc.
descendants() {
  local s c p line queue=("$@") found=()
  local -A kids=()
  for s in /proc/[0-9]*/stat; do
    { read -r line < "$s"; } 2>/dev/null || continue
    c="${s#/proc/}"; c="${c%/stat}"
    set -- ${line##*) }
    kids[$2]+=" $c"
  done
  while [ ${#queue[@]} -gt 0 ]; do
    p="${queue[0]}"; queue=("${queue[@]:1}")
    for c in ${kids[$p]:-}; do found+=("$c"); queue+=("$c"); done
  done
  [ ${#found[@]} -gt 0 ] && printf '%s\n' "${found[@]}"
  return 0
}
# A signal ends the gate: its legs (the background jobs and every process under them) are ended FIRST, so nothing
# writes into the clone while it is removed, then the EXIT trap removes it. No verdict is printed: the gate did not
# finish. The ONE build runs in the background too (`wait`), because bash runs a trap only after a FOREGROUND command
# returns, and the build is minutes long.
on_signal() {
  local jobs_now pids=()
  trap - HUP INT TERM
  echo "linux-gate: stopped by a signal; ending the legs and removing this gate's clone"
  jobs_now="$(jobs -p)"
  [ -n "$jobs_now" ] && pids=($jobs_now $(descendants $jobs_now))
  [ ${#pids[@]} -gt 0 ] && kill -TERM "${pids[@]}" 2>/dev/null
  wait 2>/dev/null
  exit "$1"
}

# The hand-off, measured with stand-ins for uname, wsl.exe and cygpath (shell functions, so no fake program and no
# setting is needed, and it runs the same on every host): a Windows host hands every argument to WSL verbatim and
# returns WSL's status; a host with no WSL is NOT RUN, never RED; a Linux host does not hand off.
if [ "${1:-}" = "--self-test" ]; then
  fails=0
  check() { if [ "$2" = "$3" ]; then echo "  ok    $1"; else echo "  FAIL  $1"; echo "        want: $3"; echo "        got:  $2"; fails=$((fails + 1)); fi; }
  got="$( uname() { echo MINGW64_NT-10.0-26300; }; cygpath() { echo 'E:\tree'; }
          wsl.exe() { printf '%s|' "$@"; echo; echo "MSYS_NO_PATHCONV=${MSYS_NO_PATHCONV:-unset}"; return 7; }
          run_on_linux --legs unit --nice; )"
  rc=$?
  check "a Windows host hands its arguments to WSL verbatim, in this tree" "$(echo "$got" | sed -n 2p)" \
        "-d|Ubuntu|--cd|E:\\tree|--exec|bash|-lc|bash scripts/linux-gate.sh \"\$@\"|linux-gate|--legs|unit|--nice|"
  check "the hand-off turns Git Bash's path rewriting off" "$(echo "$got" | sed -n 3p)" "MSYS_NO_PATHCONV=1"
  check "the hand-off's status is WSL's" "$rc" "7"
  got="$( uname() { echo Darwin; }; PATH=/nonexistent; run_on_linux --nice; )"
  rc=$?
  check "a host with no WSL is NOT RUN" "$got" "=== LINUX GATE: NOT RUN [host] (this host is Darwin, not Linux, and has no WSL to run the gate on) ==="
  check "a host with no WSL exits 2, as every NOT RUN does" "$rc" "2"
  got="$( uname() { echo Linux; }; wsl.exe() { echo "handed off"; }; run_on_linux --nice; echo "stayed" )"
  check "a Linux host runs the gate itself" "$got" "stayed"
  got="$( legs=unit,selftests; check_legs; echo "accepted" )"
  rc=$?
  check "an unknown --legs name is NOT RUN (bad arguments) before anything runs" "$(echo "$got" | tail -1)" \
        "=== LINUX GATE: NOT RUN [arguments] (bad arguments) ==="
  check "an unknown --legs name exits 2" "$rc" "2"
  got="$( legs=unit,characterization,conformance,guard; check_legs; echo "accepted ${wanted[*]}" )"
  check "the four selectable legs are accepted, in order" "$got" "accepted unit characterization conformance guard"
  # The interpreter, through the one resolver, with stand-in interpreters (shell functions the resolver's probe runs):
  # `return 1` answers the floor probe as an interpreter below 3.12 (or the Store alias) does, `return 0` as one at it.
  got="$( python3() { return 1; }; python() { return 0; }; PYTHON=""; resolve_py; echo "$PY" )"
  check "a python3 below the floor is passed over for a python at it" "$got" "python"
  got="$( pinned-python() { return 0; }; python3() { return 0; }; PYTHON=pinned-python; resolve_py; echo "$PY" )"
  check "an explicit \$PYTHON is probed first" "$got" "pinned-python"
  got="$( python3() { return 1; }; python() { return 1; }; PYTHON=""; resolve_py 2>/dev/null; echo "resolved $PY" )"
  rc=$?
  check "no runnable Python at the floor is NOT RUN, never a leg run on a wrong interpreter" "$got" \
        "=== LINUX GATE: NOT RUN [interpreter] (python-resolve.sh found no runnable Python here — run scripts/wsl/setup-wsl.sh once) ==="
  check "no runnable Python exits 2, as every NOT RUN does" "$rc" "2"
  # THE GATE'S FOOTPRINT (kb/Work PB3194), on real directories and real processes in a scratch root of its own.
  st_root="$(mktemp -d "${TMPDIR:-/tmp}/linux-gate-self-test.XXXXXX")"
  trap 'rm -rf "$st_root" "$st_root.sync"' EXIT
  # Each exit path of a gate removes its directory: its verdict (0 GREEN, 1 RED, 2 a NOT RUN after the clone) through
  # the EXIT trap, and a signal mid-way through on_signal, which first ends the legs (a background job and the process
  # under it) so nothing writes into the clone while it goes.
  for want in 0 1 2; do
    got="$( ( begin_gate "$st_root" /e/trees/wt-exit; mkdir -p "$snap" "$gtmp"; : > "$snap/built.dll"; exit "$want" ) \
            >/dev/null 2>&1; echo "rc=$?"; ls "$st_root" )"
    check "a gate that ends with status $want removes its clone, its guard scratch and its owner file" "$got" "rc=$want"
  done
  # The signal arm reads every process's /proc entry, which the gate's host (Linux, where run_on_linux puts every gate)
  # serves at once and Cygwin's emulation of it on Windows at about half a second each: there it is said, not run. It
  # runs in the Linux gate's selftests leg and in CI's audits job on Linux.
  if [ "$(uname -s)" != "Linux" ]; then
    echo "  n/a   a gate stopped by a signal mid-way ends its legs' process trees and removes its clone" \
         "(on $(uname -s); it runs on Linux, the gate's only host)"
  else
  # SYNCHRONIZED, NEVER TIMED (the owner's rule: no wall-clock limit in a test; review R1-1 of PB3194 found a 20 s
  # watchdog that failed a slow correct gate and 5 s polls that passed without testing). The leg's GRANDCHILD (a
  # process under the background job, which only on_signal's /proc walk finds) starts a child of its own, BLOCKS TERM
  # and the probe signal, and only then says READY through a fifo: the gate blocks on that before it signals itself.
  # The grandchild and its child hold the write end of a second fifo, HELD, which the arm reads to its end: end of file
  # is the moment both have exited, so the arm waits for their exit itself. After the gate has exited (on_signal sent
  # its TERMs and reaped its jobs), the arm sends the grandchild a PROBE, SIGVTALRM, so a grandchild the stop never
  # reached ends too, and says so. The grandchild takes the first of the two signals with sigwait and records TERM when
  # TERM is that one or is still pending beside it, PROBE otherwise: a blocked signal is queued, never lost, so what it
  # records is whether a TERM was ever sent to it, whatever the order its signals come in or the load the host is under.
  # ⛔ It is NOT a bash trap: the grandchild was `bash -c` with a TERM trap, and under load bash did not run it before the
  # probe's trap ended the shell, although on_signal had sent the TERM. Measured 2026-10-24: 2 of 256 runs of this arm,
  # 32 at a time, recorded PROBE while on_signal's own record showed `kill -TERM` sent to the leg, the grandchild and
  # its child with status 0 (the Linux gate's selftests leg at c89014b6b ran this arm beside the build, and so failed).
  # The grandchild is Python, the one interpreter (resolve_py: NOT RUN [interpreter] without one, as the gate itself).
  # A stop that does not end its jobs keeps on_signal's wait, and so this arm, from returning: the self-test runner
  # reports it HANG (scripts/self_tests.py, a non-observation), never a pass.
  resolve_py
  sync="$st_root.sync"
  mkdir -p "$sync" && mkfifo "$sync/ready" "$sync/held"
  got="$( ( begin_gate "$st_root" /e/trees/wt-signal; mkdir -p "$snap"
            ( "$PY" -c '
import os, signal, subprocess, sys
marker, ready = sys.argv[1:3]
under = subprocess.Popen(["sleep", "infinity"], pass_fds=(3,))
signal.pthread_sigmask(signal.SIG_BLOCK, {signal.SIGTERM, signal.SIGVTALRM})
with open(ready, "w") as f:
    print(os.getpid(), file=f)
got = {signal.sigwait({signal.SIGTERM, signal.SIGVTALRM})} | signal.sigpending()
with open(marker, "a") as f:
    print("TERM" if signal.SIGTERM in got else "PROBE", file=f)
under.kill()
under.wait()
' "$sync/marker" "$sync/ready" 3>"$sync/held" &
              wait ) &
            leg=$!
            read -r child < "$sync/ready"; echo "$leg $child" > "$sync/pids"
            kill -TERM "$BASHPID"; wait ) >/dev/null 2>&1 &
          gate_pid=$!
          exec 5<"$sync/held"
          wait "$gate_pid"; rc=$?
          read -r leg child < "$sync/pids"
          kill -VTALRM "$child" 2>/dev/null
          cat <&5 >/dev/null; exec 5<&-
          alive=""; kill -0 "$leg" 2>/dev/null && alive=" leg"
          echo "rc=$rc alive:${alive:- none} leg-child: $(cat "$sync/marker")"; ls "$st_root" )"
  rm -rf "$sync"
  check "a gate stopped by a signal mid-way ends its legs' process trees and removes its clone" "$got" \
        "rc=143 alive: none leg-child: TERM"
  fi
  # Reaping: an ended gate's directory goes whatever its age, a live one stays; a directory in the layout before this
  # mechanism (no owner file) goes when it is this tree's or older than the bound, and stays otherwise.
  ( : ) & wait $!; dead=$!
  mkdir -p "$st_root"/{ended.1,live.2,noowner.3,old-tree,young-tree,young-tree.guard-tmp,wt-self,wt-self.guard-tmp}
  echo "$dead 12345 /e/trees/ended" > "$st_root/ended.1/owner"
  me="$BASHPID"; echo "$me $(proc_field "$me" 20) /e/trees/live" > "$st_root/live.2/owner"
  : > "$st_root/noowner.3/owner"
  touch -d "@$(( $(date +%s) - LEGACY_MAX_AGE_S - 60 ))" "$st_root/old-tree"
  reaped="$(reap_gates "$st_root" wt-self)"
  check "reaping removes the ended gate, the old and this tree's earlier directories, and keeps the live and young ones" \
        "$(ls "$st_root" | tr '\n' ' ')" "live.2 noowner.3 young-tree young-tree.guard-tmp "
  check "reaping says what it removed" "$reaped" \
        "linux-gate: removed 4 leftover gate directories under $st_root: ended.1 old-tree wt-self wt-self.guard-tmp"
  rm -rf "${st_root:?}"/*
  # The start check, over a stand-in df: it can fail, on inodes and on space, before anything is cloned.
  fake_df() {  # <free inodes> <free KB>, as `df -P` prints them
    eval "df() { case \"\$1\" in -Pi) printf 'Filesystem Inodes IUsed IFree IUse%% Mounted\n/dev/sdd 16777216 0 $1 0%% /\n' ;;
                 -Pk) printf 'Filesystem 1024-blocks Used Available Capacity Mounted\n/dev/sdd 263174212 0 $2 0%% /\n' ;; esac; }"
  }
  got="$( fake_df 777216 207000000; check_disk /x; echo "started" )"; rc=$?
  check "too few free inodes is NOT RUN [disk], before any clone" "$got" \
        "=== LINUX GATE: NOT RUN [disk] (777216 free inodes on the filesystem holding /x, below the bound $MIN_FREE_INODES, after removing every ended gate's leftovers: free them, then gate again) ==="
  check "too few free inodes exits 2, as every NOT RUN does" "$rc" "2"
  got="$( fake_df 14000000 1048576; check_disk /x; echo "started" )"; rc=$?
  check "too little free space is NOT RUN [disk]" "$got" \
        "=== LINUX GATE: NOT RUN [disk] (1 GB free on the filesystem holding /x, below the bound $((MIN_FREE_KB / 1024 / 1024)) GB, after removing every ended gate's leftovers: free it, then gate again) ==="
  check "too little free space exits 2" "$rc" "2"
  got="$( fake_df 14000000 207000000; check_disk /x; echo "started" )"
  check "room on both bounds starts the gate" "$got" "started"
  got="$( fake_df - 207000000; check_disk /x; echo "started" )"
  check "a filesystem that counts no inodes is held to the space bound alone" "$got" "started"
  got="$( df() { return 1; }; check_disk /x; echo "started" )"
  check "a filesystem df cannot measure is NOT RUN [disk], never assumed roomy" "$got" \
        "=== LINUX GATE: NOT RUN [disk] (cannot measure the free space of the filesystem holding /x: df -Pk gave no number) ==="
  if [ "$fails" -eq 0 ]; then echo "=== LINUX-GATE SELF-TEST: GREEN ==="; exit 0; fi
  echo "=== LINUX-GATE SELF-TEST: RED ($fails) ==="; exit 1
fi

run_on_linux "$@"

legs="unit,characterization,conformance,guard"
nice_prefix=()
while [ $# -gt 0 ]; do
  case "$1" in
    --legs) legs="$2"; shift 2 ;;
    --nice) nice_prefix=(nice -n 10); shift ;;   # an implementer's run yields the cores to the lander's
    *) echo "linux-gate: unknown argument '$1'"; echo "=== LINUX GATE: NOT RUN [arguments] (bad arguments) ==="; exit 2 ;;
  esac
done
check_legs

[ -d "$HOME/.dotnet" ] && export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if ! command -v dotnet >/dev/null 2>&1; then
  echo "=== LINUX GATE: NOT RUN [sdk] (no .NET SDK in this Linux environment — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
resolve_py
# tooling_check.py's self-test parses every agent and skill frontmatter with PyYAML (kb/Work PB3097, branch review R3-1):
# an interpreter without it is NOT RUN with the setup command, never a RED self-test leg.
if ! "$PY" -c 'import yaml' >/dev/null 2>&1; then
  echo "=== LINUX GATE: NOT RUN [interpreter] ($PY, $(command -v "$PY"), has no PyYAML — run scripts/wsl/setup-wsl.sh once) ==="; exit 2
fi
if [ ! -f Cobol.Net.sln ]; then
  echo "=== LINUX GATE: NOT RUN [tree] (not at a repository root: $(pwd)) ==="; exit 2
fi
tree="$(pwd)"

# THIS SCRIPT'S OWN READS OF THE TREE (never exported) go through `wgit`: the TREE'S OWN git, and never one that
# writes. kb/Work PB2880 measured the alternative: Linux git reading a Windows tree through /mnt took 34 s for the one
# `status` below (every run, at ~0.5 of one core: the gate's first serial valley). The index's stat data is Windows
# git's, so Linux git re-read and re-hashed every tracked file across the 9P boundary, and then WROTE the refreshed
# index back into the real worktree (an "optional lock"), which Windows git rewrote again on its next command. So:
#  - a tree on a Windows drive (WSL; `wslpath -w` names a drive letter) is read by Windows git (`git.exe` through WSL
#    interop) with its Windows path, against the stat data it wrote itself: 0.1-1.2 s;
#  - a tree on a Linux filesystem (a cloud session, a CI-like host) is read by Linux git, as any Linux checkout;
#  - both pass `--no-optional-locks`, so a read never refreshes the index of the repository it reads.
# Each arm reports git's paths in this shell's form (`wpath`): the clone below reads the object store through it.
winroot="$(wslpath -w "$tree" 2>/dev/null)"
case "$winroot" in
  [A-Za-z]:\\*)
    if ! command -v git.exe >/dev/null 2>&1; then
      echo "=== LINUX GATE: NOT RUN [tree] (the tree $winroot is on a Windows drive and Windows git (git.exe) is not reachable" \
           "from WSL: enable WSL interop, or Linux git re-hashes the whole tree across /mnt — kb/Work PB2880) ==="; exit 2
    fi
    wgit() { git.exe --no-optional-locks -C "$winroot" "$@"; }
    wpath() { wslpath -u "$(printf '%s' "$1" | tr -d '\r')"; } ;;
  *)
    wgit() { git -c safe.directory='*' --no-optional-locks -C "$tree" "$@"; }
    wpath() { printf '%s' "$1"; } ;;
esac
head="$(wgit rev-parse HEAD 2>/dev/null | tr -d '\r')"
common="$(wgit rev-parse --path-format=absolute --git-common-dir 2>/dev/null)"
[ -n "$common" ] && common="$(wpath "$common")"
if [ -z "$head" ] || [ -z "$common" ]; then
  echo "=== LINUX GATE: NOT RUN [tree] (cannot read this tree's HEAD and git directory: $tree) ==="; exit 2
fi
dirty="$(wgit status --porcelain --untracked-files=no 2>/dev/null | tr -d '\r' | grep -v ' STATUS.md$' | wc -l)"
# TRIPWIRE (group G's, kb/Work PB1719): nothing below may write to the REAL repository. The clone makes that true by
# construction; this snapshot makes any future breach LOUD instead of silent. (Not the worktree list: other agents add
# and remove worktrees of the shared repository while this runs.)
repo_state() { printf '%s|%s' "$(wgit rev-parse HEAD 2>/dev/null)" "$(wgit config --get core.worktree 2>/dev/null)"; }
state_before="$(repo_state)"

# THE PLANNER READS THIS DIRECTORY (gate_plan.TRX_WRITERS, kb/Work PB3155): a case red in a trx file here is tier 0a of
# this worktree's next gate, so a finisher's re-gate after a landing's Linux red runs it in leg 1. It holds only the
# newest run: emptied first.
out="$tree/TestResults/linux-gate"
rm -rf "$out"; mkdir -p "$out"
# This gate's footprint (kb/Work PB3194, above): reap what ended gates left, refuse on a full filesystem, then work in
# this gate's own directory, removed on every exit path.
mkdir -p "$GATES_ROOT"
reap_gates "$GATES_ROOT" "$(basename "$tree")"
check_disk "$GATES_ROOT"
begin_gate "$GATES_ROOT" "$tree"
if ! git -c safe.directory='*' clone --quiet --shared --no-checkout "$common" "$snap" > "$out/clone.log" 2>&1 \
   || ! git -C "$snap" -c advice.detachedHead=false checkout --quiet "$head" >> "$out/clone.log" 2>&1; then
  echo "=== LINUX GATE: NOT RUN [tree] (could not clone HEAD ${head:0:9} into $snap; see TestResults/linux-gate/clone.log) ==="; exit 2
fi
# The GPL GnuCOBOL corpus is git-ignored, so a clone never has it; ExternalCorpusPopulationDriftTests is RED without
# it by design (PB209). Copy the tree's (no network), then apply THE one corpus rule the Windows gate and the battery
# apply (scripts/external_corpus.py, kb/Work PB2611): fetch when still absent, and a failed fetch is a named red.
corpus_red=""
if [ -d tests/external/gnucobol ]; then
  mkdir -p "$snap/tests/external" && cp -r tests/external/gnucobol "$snap/tests/external/"
fi
if ! "$PY" "$snap/scripts/external_corpus.py" ensure --repo "$snap" > "$out/corpus-fetch.log" 2>&1; then
  echo "linux-gate: EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED (TestResults/linux-gate/corpus-fetch.log); the two ExternalCorpusPopulationDriftTests reds in the unit leg are attributable to it"
  corpus_red=" corpus-fetch"
fi
echo "linux-gate: Linux clone of HEAD ${head:0:9} at $snap"
# THE INERT LANE (kb/Work PB2878): the change is HEAD against its merge base with the tree's origin/main (the clone
# shares the object store, so that commit resolves in it). No origin/main, or a change git cannot list, is the full lane.
main_sha="$(wgit rev-parse --verify --quiet origin/main 2>/dev/null | tr -d '\r')"
lane_base="$( [ -n "$main_sha" ] && git -C "$snap" merge-base HEAD "$main_sha" 2>/dev/null )"
if [ -n "$lane_base" ]; then
  lane_out="$("$PY" "$snap/scripts/inert_lane.py" classify --root "$snap" --base "$lane_base" --head HEAD 2>&1)"
else
  lane_out=$'lane=full\nreason=no merge base with origin/main: the full lane'
fi
lane_reason="$(printf '%s\n' "$lane_out" | sed -n 's/^reason=//p')"
if printf '%s\n' "$lane_out" | grep -qx 'lane=inert'; then
  echo "linux-gate: inert lane — $lane_reason"
  echo "=== LINUX GATE: GREEN (inert lane, kb/Work PB2878: $lane_reason; no leg runs; HEAD ${head:0:9}) ==="; exit 0
fi
echo "linux-gate: full lane — ${lane_reason:-$lane_out}"
[ "$dirty" -gt 0 ] && echo "linux-gate: NOTE — $dirty uncommitted tracked change(s) are NOT tested (the clone is HEAD)"

bad="$corpus_red"; ran=""
# PB2879 (owner 2026-10-09): the legs ran one after another and used about 10 % of a 32-CPU WSL VM (351 s alone). The
# clone is now built ONCE (`dotnet build Cobol.Net.sln`) and every leg runs in PARALLEL against that build, each to its
# own log. A background leg cannot set this shell's variables, so each writes its report lines to <leg>.result and
# GREEN|RED|NOT RUN to <leg>.status; after `wait` they are printed in a fixed order and folded into the verdict.
# `selftests` needs no build, so it starts beside the build. Every script self-test runs on Linux through the ONE runner
# CI runs (kb/Work PB2563): `selftests` is `"$PY" scripts/self_tests.py`, `selftests-built` its `--built` half, which
# reads the built test assemblies. They are path-sensitive: a drive-letter path that Linux reads as relative turned CI
# red on 2026-10-07 after a green Windows run (kb/Work PB2142).
leg_status() { printf '%s\n' "$2" > "$out/$1.status"; }   # <leg> <GREEN|RED|NOT RUN>
selftests_leg() {  # <leg name> <clone> [runner arguments…]
  local name="$1" dir="$2"; shift 2
  local start rc secs verdict
  start=$(date +%s)
  ( cd "$dir" && "${nice_prefix[@]}" "$PY" scripts/self_tests.py "$@" ) > "$out/$name.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  verdict="$(grep -E '^=== SELF-TESTS: ' "$out/$name.log" | tail -1)"
  if [ $rc -eq 0 ] && [[ "$verdict" == "=== SELF-TESTS: GREEN"* ]]; then
    leg_status "$name" GREEN; echo "leg $name: GREEN in ${secs}s — $verdict"
  else
    # A red runner prints its WHOLE log less the self-tests that passed: trim what passed, never what failed (PB1573).
    leg_status "$name" RED
    echo "leg $name: RED (rc=$rc) in ${secs}s — ${verdict:-no self-test verdict; see TestResults/linux-gate/$name.log}"
    grep -vE '^self-test .*: GREEN in ' "$out/$name.log" | sed 's/^/    /'
  fi
}
guard_leg() {
  # CI's `guard` job (kb/Work PB1955, PB1957 row 40): the NIST suite through the `cobol` CLI from bash, the manifest
  # audit, the baseline check and the guard's own self-tests — the one CI Linux job that runs a SCRIPT rather than
  # `dotnet test`, so it was the one no local gate ran. Train 1013's CI red on PB322's TERMINATES rows was invisible to
  # every local leg. Its scratch is private to this clone (guard-fast.sh's own mktemp directory and every
  # group's lie under TMPDIR, and implementers run this gate concurrently; kb/Work PB2922). It runs in the AUDIT clone
  # (kb/Work PB2878), whose build its own `dotnet build` of the CLI finds up to date (kb/Work PB3529).
  # ⛔ The scratch lies BESIDE the clone, never inside it (kb/Work PB2989): `selftests-built` runs at the same time and
  # keys its inputs by every untracked file of the clone (scripts/self_test_inputs.py), so a guard temp file listed by
  # `git ls-files -o` and deleted before `git hash-object` read it made that leg NOT RUN (train 1056b, 2026-10-11).
  # Its forensics (the evidence of every non-MATCH) go to the tree's TestResults, the one place a gate's results outlive
  # the clone (kb/Work PB3194); guard-fast.sh then removes its own scratch whatever the verdict.
  local start rc secs summary
  start=$(date +%s)
  mkdir -p "$gtmp"
  ( cd "$auditsnap" && TMPDIR="$gtmp" GUARD_FORENSICS="$out/guard-forensics" "${nice_prefix[@]}" bash scripts/guard-fast.sh ) \
    > "$out/guard.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^=== (NIST \(|NIST AUDIT: population|ALL GREEN|FAILURES)' "$out/guard.log" | tr '\n' ' ')"
  if [ $rc -eq 0 ] && grep -qx '=== ALL GREEN ===' "$out/guard.log"; then
    leg_status guard GREEN; echo "leg guard: GREEN in ${secs}s — $summary"
  else
    # A red guard prints its WHOLE log less the per-program verdicts that came out as predicted: trim what passed,
    # never what failed (kb/Work PB1573). The audit's findings and every non-matching verdict's evidence stay.
    leg_status guard RED
    echo "leg guard: RED (rc=$rc) in ${secs}s — ${summary:-no guard verdict; see TestResults/linux-gate/guard.log}"
    grep -vE '^ *[A-Z][A-Z0-9]+: (MATCH|TERMINATES EC-|NO BASELINE)' "$out/guard.log" | sed 's/^/    /'
  fi
}
test_leg() {  # <leg> <project> — `dotnet test --no-build` against the ONE build
  local leg="$1" proj="$2" start rc secs summary
  start=$(date +%s)
  # Scrubbed (kb/Work PB1718): a gate-leg handshake or VSTest* variable inherited from the caller would narrow the run
  # to PART of its assembly while it exits 0. The one statement of that rule is test_population.py.
  # The harness's observation log (tests/_shared/HarnessLog.cs: every retry and non-observation of a child process, with
  # its budget, its work and the host's load, and every scratch directory a test could not remove) is this gate's own,
  # never a temp file every gate on the host appends to (kb/Work PB3108). It is written in the gate's directory on the
  # LINUX filesystem and copied to TestResults/linux-gate as the gate ends (remove_gate), never written there directly:
  # that directory is a Windows drive mounted over 9P, where an append beside a reader of the same file is not reliable.
  # Measured 2026-10-24 under WSL on /mnt/e: one process appending (open, seek to the end, write, close) while another
  # re-read the file lost 8 to 14 of 800 lines per run, and the reader's read failed with ENODATA ("No data available");
  # the same run on the Linux filesystem lost none and failed none. HarnessLogTests reads the log beside its writers,
  # and on /mnt/e its read failed (the landing at c89014b6b, kb/Work PB3194).
  ( cd "$snap" && export COBOLNET_HARNESS_LOG="$gate/$leg.observations.log" \
      && "${nice_prefix[@]}" "$PY" "$snap/scripts/test_population.py" scrubbed dotnet test "$proj" \
      --no-build --no-restore --logger "trx;LogFileName=$leg.trx" --results-directory "$out" ) > "$out/$leg.log" 2>&1
  rc=$?
  secs=$(( $(date +%s) - start ))
  summary="$(grep -E '^(Passed!|Failed!)' "$out/$leg.log" | tail -1)"
  if [ $rc -eq 0 ] && grep -qE '^Passed!' "$out/$leg.log"; then
    leg_status "$leg" GREEN; echo "leg $leg: GREEN in ${secs}s — $summary"
  else
    leg_status "$leg" RED
    echo "leg $leg: RED (rc=$rc) in ${secs}s — ${summary:-no test summary; see TestResults/linux-gate/$leg.log}"
    grep -E '^\s+Failed ' "$out/$leg.log" | head -20 | sed 's/^/    /'
  fi
}

# THE AUDIT CLONE (kb/Work PB2878): the script self-tests are AUDITS that read inert paths by design, and the guard is
# no `dotnet test` leg, so they run in a clone of their own, with its own build, outside the inert watch of the test
# legs below. `selftests` starts at once; the clone's build then starts `selftests-built` (it reads the unit and
# conformance assemblies) and the guard. Every one writes its leg's .status and .result like any leg.
auditsnap="$snap.audits"
rm -rf "$auditsnap"
order=(selftests)
audit_built_legs() {  # the audit clone's build, then the legs that need it
  local abrc
  ( cd "$auditsnap" && "${nice_prefix[@]}" dotnet build Cobol.Net.sln -c Debug ) > "$out/build-audits.log" 2>&1
  abrc=$?
  if [[ " ${wanted[*]} " == *" guard "* ]]; then
    if [ $abrc -eq 0 ]; then guard_leg > "$out/guard.result" 2>&1 &
    else leg_status guard RED; echo "leg guard: RED — the audit clone's build failed (rc=$abrc; TestResults/linux-gate/build-audits.log)" > "$out/guard.result"; fi
  fi
  case ",$legs," in
    *,unit,*conformance,*|*,conformance,*unit,*)
      if [ $abrc -eq 0 ]; then
        selftests_leg selftests-built "$auditsnap" --built > "$out/selftests-built.result" 2>&1 &
      else
        leg_status selftests-built RED
        echo "leg selftests-built: RED — the audit clone's build failed (rc=$abrc; TestResults/linux-gate/build-audits.log)" > "$out/selftests-built.result"
      fi ;;
    *)
      # Not run because the legs asked for build neither unit nor conformance: said, never silent.
      leg_status selftests-built "NOT RUN"
      echo "leg selftests-built: NOT RUN (it reads the unit and conformance assemblies, and --legs '$legs' asks for" \
           "neither of them)" > "$out/selftests-built.result" ;;
  esac
  wait
}
if git -c safe.directory='*' clone --quiet --shared --no-checkout "$common" "$auditsnap" >> "$out/clone.log" 2>&1 \
   && git -C "$auditsnap" -c advice.detachedHead=false checkout --quiet "$head" >> "$out/clone.log" 2>&1; then
  selftests_leg selftests "$auditsnap" > "$out/selftests.result" 2>&1 &
  audit_built_legs &
else
  for leg in selftests guard selftests-built; do
    leg_status "$leg" RED
    echo "leg $leg: RED — could not clone HEAD ${head:0:9} into $auditsnap (TestResults/linux-gate/clone.log)" > "$out/$leg.result"
  done
fi
start=$(date +%s)
( cd "$snap" && "${nice_prefix[@]}" dotnet build Cobol.Net.sln -c Debug ) > "$out/build.log" 2>&1 &
wait $!   # in the background, so a signal reaches on_signal during the build (bash defers a trap past a foreground command)
brc=$?
echo "linux-gate: ONE build of Cobol.Net.sln in $(( $(date +%s) - start ))s (rc=$brc); the test legs run in parallel against it"
# THE ONE BUILD IS FINAL (kb/Work PB3529). The legs run the CLI, which is safe only while nothing builds this clone again
# (the guard's own `dotnet build` of the CLI now runs in the audit clone, kb/Work PB2878). A fresh clone's second build once recompiled the frontend
# and everything above it (the generated parser joined Compile after the SDK hashed it), replacing the `cobol` apphost
# while the conformance leg ran it: "No such file or directory", "Text file busy". The fix is the build's own
# (Cobol.Net.Frontend.csproj, AntlrGenerationDriftTests); this fingerprint of the CLI's output names a recurrence as
# itself instead of as a red in whichever leg ran the binary at that moment.
cli_fingerprint() { find "$snap/src/Cobol.Net.Cli/bin" -type f -printf '%i %T@ %P\n' 2>/dev/null | sort; }
cli_before="$(cli_fingerprint)"
if [ $brc -ne 0 ]; then
  echo "linux-gate: the Linux BUILD failed; see TestResults/linux-gate/build.log"
  grep -E ' error ' "$out/build.log" | sort -u | head -10 | sed 's/^/    /'
fi
# THE INERT WATCH (kb/Work PB2878): every inert path of the clone is watched while the legs run; the watcher's command
# waits for the stop file this script writes once the legs end. Its readiness is its events file.
watch_events="$out/inert-watch.events"; watch_stop="$out/inert-watch.stop"; watch_pid=""
rm -f "$watch_events" "$watch_stop"
if [ $brc -eq 0 ]; then
  ( cd "$snap" && "$PY" scripts/inert_lane.py watch --root "$snap" --events "$watch_events" \
      -- bash -c 'while [ ! -e "$1" ]; do sleep 1; done' inert-watch "$watch_stop" ) > "$out/inert-watch.log" 2>&1 &
  watch_pid=$!
  for _ in $(seq 1 240); do [ -e "$watch_events" ] || ! kill -0 "$watch_pid" 2>/dev/null && break; sleep 0.25; done
fi
for leg in "${wanted[@]}"; do
  order+=("$leg")
  case "$leg" in
    unit)             proj="tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj" ;;
    characterization) proj="tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj" ;;
    conformance)      proj="tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj" ;;
    guard)            continue ;;   # the audit clone runs it (audit_built_legs)
  esac
  if [ $brc -ne 0 ]; then
    leg_status "$leg" RED; echo "leg $leg: RED — the Linux BUILD failed (rc=$brc)" > "$out/$leg.result"; continue
  fi
  test_leg "$leg" "$proj" > "$out/$leg.result" 2>&1 &
done
# selftests-built runs in the audit clone, after that clone's build (audit_built_legs).
order+=(selftests-built)
# Every leg, never the watcher: it ends only once the stop file exists.
for job in $(jobs -p); do [ "$job" = "$watch_pid" ] || wait "$job"; done
if [ -n "$watch_pid" ]; then
  order+=(inert-watch)
  touch "$watch_stop"
  wait "$watch_pid"; wrc=$?
  wverdict="$(grep -E '^=== INERT WATCH: ' "$out/inert-watch.log" | tail -1)"
  if [ $wrc -eq 0 ] && [[ "$wverdict" == "=== INERT WATCH: GREEN"* ]]; then
    leg_status inert-watch GREEN; echo "leg inert-watch: GREEN — $wverdict" > "$out/inert-watch.result"
  else
    leg_status inert-watch RED
    { echo "leg inert-watch: RED (rc=$wrc) — ${wverdict:-no watch verdict; see TestResults/linux-gate/inert-watch.log}"
      sed 's/^/    /' "$out/inert-watch.log"; } > "$out/inert-watch.result"
  fi
fi
for leg in "${order[@]}"; do
  cat "$out/$leg.result"
  st="$(cat "$out/$leg.status" 2>/dev/null)"
  case "$st" in
    GREEN) ran="$ran $leg" ;;
    "NOT RUN") ;;   # only selftests-built is ever NOT RUN, when --legs asks for neither assembly it reads (said in its line)
    *) bad="$bad $leg" ;;
  esac
done
[ $brc -ne 0 ] && bad="$bad build"
if [ $brc -eq 0 ] && [ "$(cli_fingerprint)" != "$cli_before" ]; then
  echo "build: RED — the CLI's output changed after the ONE build while the legs ran (a second build of the same tree" \
       "was not a no-op, kb/Work PB3529); a leg that ran the CLI meanwhile may have failed for that alone. Changed:"
  diff <(printf '%s\n' "$cli_before") <(cli_fingerprint) | grep -E '^[<>]' | head -10 | sed 's/^/    /'
  bad="$bad build-rewritten"
fi

state_after="$(repo_state)"
if [ "$state_after" != "$state_before" ]; then
  echo "repository: RED — something wrote to the REAL repository (HEAD|core.worktree before: $state_before," \
       "after: $state_after). Undo it, then find what escaped the clone."
  bad="$bad repository-written"
fi

if [ -z "$bad" ]; then
  echo "=== LINUX GATE: GREEN (legs$ran; HEAD ${head:0:9}) ==="; exit 0
fi
echo "=== LINUX GATE: RED (failed or not run:$bad; HEAD ${head:0:9}; logs in TestResults/linux-gate/) ==="; exit 1
