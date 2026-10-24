## PB3610: merge drivers for the files two landings both edit, and the runtime ABI pin as a measured file

Landings stopped at CONFLICT on files that are not code, and each stop cost a finisher cycle (about 100 k tokens and a
re-gate) to do what a program does exactly. The counts, from the 487 landing logs under `E:\COBOL-coord\logs`
(`python scripts/merge_drivers.py --conflicts <logs>`): `tests/conformance/2002/manifest.json` 15 and
`tests/conformance/negative/manifest.json` 13 (both gone since the conformance suite replaced them),
`conformance-suite/wiseowl/diagnostics.json` 4, `RuntimeAbiPinDriftTests.cs` 4, the traceability inventory 2, six
`kb/Work` notes. The remaining conflicts are prose docs and source files, which are real.

**The mechanism.** `scripts/merge_drivers.py` holds a registry of git merge drivers (`DRIVERS`), `.gitattributes`
repeats its patterns, and `--check` plus `MergeDriverDriftTests` keep the two together. git runs a driver only when
both sides changed a file and only when the merge passes `-c merge.<name>.driver=...`, so the one function that merges
main in (`merge_drivers.merge`) passes them on the command line: nothing is installed in any checkout, so nothing goes
stale. `enqueue.merge_main` calls it; so do the finisher, golden-lane and predecessor-merge instructions
(`plan_wave.py`, `wf_rolling_wave.js`, the two briefs), because a bare `git merge` falls back to a line merge.

- **Keyed catalogues** (`wiseowl/diagnostics.json`, `options.json`, the traceability inventory, `constructs.json`): the
  entries are located from the JSON structure, not the indentation (constructs.json has entries written at three
  indentations; diagnostics.json holds raw § beside `\u00a7`, so a parse and re-serialize would rewrite 400 KB), merged
  three-way per key and kept byte for byte. A key both sides changed differently, or one changed and one removed, is a
  real conflict and falls back to git's line merge so the finisher sees the usual markers. Round trip of the four real
  files is the identity (2 s for the 7.5 MB inventory).
- **Work notes** (`kb/Work/PB*.md`): the frontmatter per key, the body by git's union merge.
- **The runtime ABI pin** is now a measured file, `src/Cobol.Net.Runtime/RuntimeAbi.pin`, replacing four constants in
  `RuntimeAbiPinDriftTests.cs`. The test writes the measurement under `COBOLNET_WRITE_ABI_PIN=1` (the diagnostics doc has
  the same switch); `scripts/abi_pin.py bump --runtime-major|--call-abi|repin` is the author's one command and replaces
  three hand edits and a copied hash. Its merge driver ADDS the counters two branches each raised (a finisher derived
  exactly this on 2026-10-24: "main's 17 shipped AnnuityDec(..., long), so the merged surface is an incompatible change
  against main's 17: major 18") and marks the hashes UNMEASURED, because a hash is a property of the merged tree that no
  branch measured; `merge_drivers.merge` runs `abi_pin.py settle` (the counters into `Directory.Build.props` and
  `RuntimeAbi.cs`, a build, the test's write mode) BEFORE it makes the merge commit, under the shared gate slot.
- **The review's coverage rule** (`prune_worktrees.mechanical`) read `git merge-tree` without the drivers, which would now
  disagree with the merge: it reads `merge_drivers.is_drivers_merge`, so a merge the drivers made (its pin settled, in
  the three files `abi_pin.OWNED` names, and only when the drivers left the pin UNMEASURED) is mechanical and a hand
  resolution is not.

**Re-probe and the estimate.** The note's fix shape said the pin is "bumped at the LANDING from the merged tree, never by
the branch". A branch's own gate must be green on its own tree, so the branch still raises its numbers (now by one
command); what the landing owns is the merged tree's hashes and the sum of the counters, which is what every conflict on
the pin required. `abi_pin.py repin` on this tree re-measured the pin the old constants held (681D9891A9A51E45,
82F4263ED6B4BC0A): the move changed nothing a module records.

**Goldens.** `merge_drivers.py --self-test` (the keyed arms, the note arm, a scratch repository whose two branches each
add an entry, a real conflict, keep, the pin end to end with a stand-in measurement, the coverage rule, the registry
check), `abi_pin.py --self-test`, one new `enqueue.py --self-test` arm (both sides adding a member to a keyed catalogue is
merged by key where a plain git merge conflicts), `MergeDriverDriftTests` (arms declared, registry equals
`.gitattributes`, every merge site goes through the drivers, the pin is measured on the tree), and the plan-wave and
rolling-wave tests follow the new instruction text.

**Self-review.** Found and fixed before the gate: a `--` inside the XML comment I added to `Directory.Build.props` made the
whole solution unrestorable (NETSDK1013, found by the first solution build); a `git merge --abort` that keeps the settle's
own edits to files the merge changed (restored from HEAD); a union merge that returned an empty body when git failed
(now a conflict); an unchecked `git add` in the settle.

**Gate.** The first gate (000001-20261024T173719Z) was RED in Unit leg 1 on three drift tests that read what the new files are:
`HostWitnessTraitDriftTests` (MergeDriverDriftTests names paths under scripts/, so drift_rules.py makes it a tree-wide sweep and
it must carry the Sweep trait), `FilterPopulationGuardDriftTests` and `GateLegDriftTests` arm 6 (abi_pin.py runs a filtered
`dotnet test`: it now asserts the filter selects something with filter_population.py and runs scrubbed of the gate environment,
and arm 6 lists it among the sites the caller audit must see). The three are fixed on the branch.
