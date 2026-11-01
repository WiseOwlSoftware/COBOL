## Entry 2912 — 2026-10-31 21:14 PDT — kb/Work: file PB4350 (PB4327's stale-doc review leads) and PB4351 (the differential's Windows-only apphost default)

PB4350 records four sites the doc sweep of PB4327 (the Linux half leaves the per-change gate) left describing the deleted Linux half as current: MANDATORY-PRACTICES row L2, the README `linux-gate.sh` paragraph, two places in DESIGN-orchestrator-loop (the `land_lane.py --self-test` paragraph and the 16.7 `lane_decide.py` row) and the `InertLaneDriftTests` summary. It came from the branch review of PB4327 (leads R1-1 to R1-4). CLAUDE.md and DESIGN-test-build-ci.md also still describe it, but PB4334 rewrites those whole, so the note leaves them out. It is blocked by PB4327.

PB4351 records that the built CLI apphost path is hand-written in each script: `gnucobol_differential.py` defaults `--exe` to the Windows `cobol.exe`, so the differential cannot run on Linux without `--exe`, while `differential_fuzz.py` tests `os.name` itself. It came from the PB4332 finisher's report (lead 2) and was mentioned untracked in PB4346's Related paragraph.

The other leads of the Mythos lane's waves 20261790, 20261792 and 20261793 were already notes (PB4319, PB4320, PB4321, PB4336, PB4346, PB4348, PB4349) or fixed on their branches.

