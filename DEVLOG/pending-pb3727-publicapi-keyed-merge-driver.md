## PB3727: the public-API baselines are merged as sets of symbol lines (cobol-keyed-publicapi), not by line union

**What changed.** kb/Work PB3727 is tier 1 of the PB3726 restructure, and DESIGN-orchestrator-loop.md section 16.9 is
its design. `src/**/PublicAPI.*.txt` no longer merges by `merge=union`. It merges through a new driver,
`cobol-keyed-publicapi`, in `scripts/merge_drivers.py`. `merge_symbol_set` treats a baseline as a SET: a line either
side added is kept, and a line either side removed is gone. The `#nullable enable` header stays first and the symbol
lines follow in ordinal order. The driver never declines, because a line is atomic and a stale symbol already fails the
build (RS0017).

**Re-probe.** `git merge-file --union` on three-line baselines showed both defects. When one side removed `A.Y` and the
other added `A.N` beside it, the union kept `A.Y`. When both sides added `A.D` at different places, the union kept it
twice. The second is the PB3694 duplicate behind the RS0025 bounce of #541.

**What else moved.**
- `.gitattributes` names the new driver, with its reason.
- Both runtime baselines were sorted once, keeping the same sets.
- `PublicAPI.Shipped.txt` order feeds the runtime ABI pin's `runtime-hash`, so the pin was repinned with
  `abi_pin.py repin` (a reorder, nothing incompatible).
- The registry now matches driver patterns by git's rules. The new `merge_drivers.pattern_matches` is read by
  `covered_by_driver` and `--check`. Before, `pathlib.PurePath.match` let `**` span exactly one directory, which would
  have missed a baseline deeper in `src/`.
- **Merges now read their attributes from the registry.** A branch cut before this landing still says
  `merge=union` in its own `.gitattributes`, which git reads by default. Simulated against this head, such a branch,
  having added two symbols, came out with 31 duplicated lines (RS0025 in the Release build). The new
  `merge_drivers.attributes_tree` writes a one-file tree: the merged-into side's `.gitattributes` followed by a
  `merge=<driver>` line per registry pattern, where the later line wins. `merge`, `merge_conflicts` and
  `is_drivers_merge` pass it as `-c attr.tree=…`. `config_args` already supplied the driver commands this way, so a
  branch cut before a driver existed now merges that driver's files through it.
- Because the baselines are now files merged by key, the planner's collision rule and the landing's merge-overlap rule
  both set them aside, as they already do for the catalogues.
- DESIGN-orchestrator-loop.md section 16.9 records the two corrections. DESIGN-test-build-ci.md section 3.17.2 step 2
  lists the new driver, and DOC_INDEX gains a row for `merge_drivers.py` and `abi_pin.py`.

**Arms.** The self-test gained the four arms section 16.9 names, a git-path arm, an old-attributes arm and a pattern
arm. `MergeDriverDriftTests` declares all seven. With `attributes_tree` stubbed out, the old-attributes arm fails with
`D.D` twice.

**Oracle.** The three latest main merges in which both parents changed a baseline were 009f47b01, cdd86463e and
677c620a0. Re-run through the driver, each reproduces main's baseline as a set.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN ===` on 502c2fd01 (run 000001-20261025T211812Z): Conformance 8,305, Unit 36,573 and Characterization 8 cases ran, and the Linux half (a WSL clone of HEAD) was GREEN. The self-tests of `merge_drivers`, `enqueue`, `prune_worktrees`, `land_lane` and `test_plan_wave` re-ran GREEN, and all four `MergeDriverDriftTests` facts passed.
