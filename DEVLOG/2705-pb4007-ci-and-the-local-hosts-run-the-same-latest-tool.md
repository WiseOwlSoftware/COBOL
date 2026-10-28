## Entry 2705 — 2026-10-28 13:36 PDT — PB4007: CI and the local hosts run the same, latest tool versions (owner decision); file the gap #734 fell through

The merge queue removed #734 (PB3806, PB3991, PB3807) after its local gate and branch review passed: in merge-group run
40754265548 `scripts/spec/impact_store.py --self-test` failed two prune-race checks on CI's Windows interpreter, the runner
image's Python 3.12, where `Path.exists` goes through the `Path.stat` the test replaces; every local leg runs 3.14, where it
does not. The owner then ruled: "CI tooling should match local tooling versions - always", and "CI and local should always
be using the latest version". PB4007 records the decision, the measured differences (Python, JDK, pwsh, the SDK on WSL, the
unpinned `windows-latest`, and the local hosts disagreeing with each other) and the acceptance: one toolchain manifest kept
at the latest release, installed from by CI and every host, held by a drift test. PB1957 gains a dated section naming the
rows it settles, and CLAUDE.md's CI invariant paragraph states the rule. The test fix itself belongs to #734's finisher.

