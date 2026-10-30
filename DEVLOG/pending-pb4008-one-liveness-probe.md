## A process that has exited is dead to every lane, however a listing still names it (kb/Work PB4008)

`impact_lane.py --self-test` failed one check in CI's Windows self-tests three times, and each time it threw an unrelated
pull request out of the merge queue: #738 on 2026-10-28, #755 on 2026-10-29 and #801 on 2026-10-30 (merge-group run
41004518958). The check was "a dead worker's pid is not alive (got True, want False)". The lanes decided whether a pid was
alive from the process LISTING and its command line. They never asked whether the process had exited. A Windows process
that has exited while another handle keeps its object can still be listed under `python.exe`, and its command line can no
longer be read. The lanes take an unreadable command line as alive, by design (PB3113), so the dead worker read as a live
one. A Linux zombie fails the same way: it keeps its /proc name and has an empty command line.

The repository already had the right question in `gate_slot._pid_alive`: on Windows `WaitForSingleObject(h, 0)` on a
`SYNCHRONIZE` handle, on Linux `kill(pid, 0)` and the zombie state. This change makes that the only probe:
- `detached.pid_alive` holds the probe now. `gate_slot._pid_alive` is deleted, `detached.pid_alive` no longer forwards to
  it, and `gate_slot.py` and `run_gate_legs.py` import the probe from `detached`. The branch review found that the probe
  raised `PermissionError` on Linux for another user's live process (pid 1, for instance), and that on Windows the same
  kind of process read as dead. Now a process that exists but cannot be opened counts as running on both systems, and
  the probe never raises.
- `coord.process_name` returns None unless the probe says the process is still running. `process_command` and
  `loop_state` (the supervisor lock) get the same answer through it.
- `coord.runs_python(pid, mark)` is now the one rule by which a lane tells its own detached process from a dead one or
  from a stranger on a reused pid. `land_lane.runner_alive` and `impact_lane.worker_alive` each had their own copy of
  that rule, and now both call it.

The new arm in `coord.py --self-test` creates the state directly and never waits on a timer. A Python child exits on EOF
while its object is still held. On Windows the `Popen` keeps its handle, and the arm adds the pid to the listing, as the
CI runner's snapshot did; this host's snapshot drops an exited process at once (0 of 20 listed). On Linux,
`waitid(WNOWAIT)` leaves the zombie unreaped. Against main's `coord.py`, both Windows and WSL read the dead child as
alive (`cmd=''`). On this branch both read it as dead.

The two PowerShell readers of the supervisor lock use `Get-Process -Id`, which is also a listing. They belong to the open
PB3032, which now names this second reason. `DESIGN-orchestrator-loop.md` §16.7 still listed a `pid_alive` in
`land_lane.py`, a function PB3113 had renamed `runner_alive`; the entry now names `runner_alive`.

Branch review, round 1 (two findings, both answered). `detached.pid_alive` first raised `PermissionError` on POSIX for a
live pid owned by another user (`os.kill(pid, 0)` answers EPERM), which crashed `worker_alive`, `runner_alive`,
`loop_state` and `live_lock` on a reused pid owned by root. It now reads EPERM as "it exists" and goes on to the zombie
check, and never raises. The review's second finding was a comment that said an unopenable live process reads alive on
Windows while `process_name` read it dead. The probe is now symmetric: `OpenProcess` failing with ERROR_ACCESS_DENIED means
the pid exists and reads as running, as EPERM does. Arm 5 of `detached.py --self-test` covers both systems.
