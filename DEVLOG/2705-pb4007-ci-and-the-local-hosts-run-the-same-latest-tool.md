## Entry 2705 — 2026-10-28 13:36 PDT — PB4007: CI and the local hosts run the same, latest tool versions (owner decision); file the gap #734 fell through

The merge queue removed #734 (PB3806, PB3991, PB3807) after its local gate and branch review passed: in merge-group run
40754265548 `scripts/spec/impact_store.py --self-test` failed two prune-race checks on CI's Windows interpreter, the runner
image's Python 3.12, where `Path.exists` goes through the `Path.stat` the test replaces; every local leg runs 3.14, where it
does not. The owner then ruled: "CI tooling should match local tooling versions - always", and "CI and local should always
be using the latest version". PB4007 records the decision, the measured differences (Python, JDK, pwsh, the SDK on WSL, the
unpinned `windows-latest`, and the local hosts disagreeing with each other) and the acceptance: one toolchain manifest kept
at the latest release, installed from by CI and every host, held by a drift test. PB1957 gains a dated section naming the
rows it settles, and CLAUDE.md's CI invariant paragraph states the rule. The test fix itself belongs to #734's finisher.

The owner then ordered the tools upgraded first, by winget on Windows, with the older versions removed. Both hosts now run
Python 3.15.2, Oracle JDK 27.0.1, .NET SDK 10.0.403, pwsh 7.6.9, git 2.56.0, gh 2.103.3, jq 1.8.2 and uv 0.13.6, and all 129
script self-tests are GREEN under 3.15.2. PB4007 gains a dated section with the table and the facts its implementation must
carry: semgrep on its own 3.14, the WSL git source an upgrade had disabled, and `setup-wsl.sh`'s hard-coded 3.14.

The first enqueue of this branch was removed from the merge queue by an unrelated red: `impact_lane.py --self-test` failed "a
dead worker's pid is not alive" on CI's Windows runner (run 40765533118). `impact_lane.worker_alive` and
`land_lane.runner_alive` judge liveness from the process snapshot and the command line, never from the exit state that
`gate_slot._pid_alive` already reads; an exited process whose handle is still held can read as alive. The arm passes 3 of 3
here under Python 3.12 and 3.15.2, so it is a race. Filed as PB4008.
