## PB4350 - PB4327's doc sweep: the last current-tense descriptions of the deleted Linux half of the per-change gate

PB4327 removed the Linux half of `build-local`; its branch review found four sites that still described it. Re-probed on a
tree cut from b56ecd0be: three reproduced, one did not. MANDATORY-PRACTICES row L2 said the gate ran "the same plan's legs
in a WSL clone" (now "leg 1 on Windows, under one slot; nothing on Linux"); the README `linux-gate.sh` paragraph said
`build-local` runs the Linux half with `--legs targeted`, a form that now exits 2 (it now says the per-change gate runs
nothing on Linux); DESIGN-orchestrator-loop's `land_lane.py --self-test` paragraph named `linux-gate.sh --probe` as exit
12's second source, and the 16.7 `lane_decide.py` row listed the deleted `linux_gate_holder` (both removed). The fourth,
the `InertLaneDriftTests` class summary naming one script twice, had already been rewritten by PB4326's work. The sibling
sweep found one more: `references/landing.md` section 3 still said the landing runs the ordered gate and CI's Linux legs on
the merged head; it now says the landing reads the branch's gate record (L2). `CLAUDE.md` and `DESIGN-test-build-ci.md`
are PB4334's. Documentation only: no code, no diagnostic, no golden, no inventory row (`closes_rows: []` with its reason).
