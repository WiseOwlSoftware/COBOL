## Entry 2677 — 2026-10-27 16:12 PDT — PB3973: two self-tests wait for a spawned child to run before they read its command line

A notes-only gate went red on Linux in `impact_lane.py --self-test`, the check that a live Python that is not the
worker reads as a stranger. The same branch was green one gate before and one gate after. The cause is a race in the
test, not in the code under test. `subprocess.Popen` returns when `execve` closes the child's close-on-exec pipe, and
on Linux that happens before the new image's argument area is set, so the child's `/proc/<pid>/cmdline` can still read
empty: 78 of 300 spawns idle under WSL, 55 under CPU load. `worker_alive` treats an unreadable command line as alive,
deliberately, so the stranger read as the worker.

The spawned sleepers now print `ready` once Python runs, and the check reads that line first: 0 of 300 spawns read
empty after it. `land_lane.py`'s test 10b had the same shape and gets the same fix. The other self-tests that spawn a
child already wait for a handshake or only check that it ended.

