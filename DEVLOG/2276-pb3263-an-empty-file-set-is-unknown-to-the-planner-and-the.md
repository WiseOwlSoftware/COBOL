## Entry 2276 — 2026-10-19 17:21 PDT — PB3263: an empty file set is UNKNOWN to the planner and the landing check, and a note filed into a cluster declares its sites

Wave 1131 G (Opus; the third attempt on PB3263, the first two being notes-only). The note's fix shape has four
items, and the branch builds all four.

**Re-probe.** On the base `c82c9213f`, `work.note_sites` found no site block in 198 open notes naming 218 clusters (24
in PB3353, 6 in PB3160). `plan_wave.inflight_file_sets` dropped every dispatch-ledger group recorded with no files,
so the planner never saw it, and `plan()` met a group whose own set was empty with nothing. Wave 1126 C (PB1587, no
site block) was planned that way beside the in-flight storage engine and landed on six of its files.

**1. The planner.** `plan_wave.collision` is now the one meeting rule. Two known sets meet on a shared file. An empty
set is UNKNOWN: it meets the other side on a shared subsystem (the notes' `area:`, `plan_wave.subsystems`), and it meets
everything when the other side's subsystem is unknown too. A meeting by subsystem waits even for finished work,
because a continuation would merge a branch the group may share nothing with. Within a wave an unknown group is
planned alone on its subsystems by the same rule: it follows every group of the wave it meets, and a later group that
meets it joins its chain. A chain is linear, so when the groups it meets lie in two chains it waits for a later wave.
The first version followed only the last group on a subsystem, and the branch review (finding R1-1) showed the hole:
two known `runtime/io` groups on `Engine.cs` and `Locks.cs` are two chains, and an unsited `runtime/io` group placed
after the second ran beside the first. Four cases now pin it (two chains, so it waits; known groups after an unknown
one; an unknown subsystem meets every group, both ways), and three of them were red on the reviewed head. A dispatch recorded with no files stays in flight with its notes, and the branch survey's in-flight slugs
now count it too, which they missed before. A finisher's set now includes the files of the branch it continues. The
plan table and the spec name the group `UNPARTITIONED` and ask its implementer for a `**Sites:**` paragraph. The
external-repository slices owner now carries its notes, so an unknown group meets it only on a shared subsystem. A dry
run of `--cluster PB3353` on the branch plans PB3438, whose only site is a golden outside the campaign roots, as one
UNPARTITIONED group on `tests/conformance`. Before the slices owner carried its notes it waited on the slices.

**2. The register.** `work.py check` (`sites_problems`) refuses an open note filed into any cluster without a site
block, from `SITES_REQUIRED_FROM` = PB3443 on (the next unallocated id). The rule names no campaign, because any cluster
becomes one when an operator runs it. A `decision` note is exempt. `**Sites:** none (why)` declares a note that
changes no code file (`NoteSites.declared`). Applied to every open note, the rule would have refused 198 at once, most
in idle clusters, so those notes stay readable and the planner reads each as UNKNOWN. The registrar brief now asks
for the block.

**3. Backfill.** The 27 open notes of the running campaigns that had no block got one from their own text: 21 in
PB3353 and 6 in PB3160. PB3353 is left with its two decision notes and PB3263, and PB3160 with none. PB3337's and
PB3338's `## Files` lists used brace and glob paths (`IO/{FileRegistry,…}.cs`, `Binding/*`), which the one reader
rejects, so the backfill expands them.

**4. The landing check.** In arm 1, an R3 note that no ledger entry records, closed on the way by the landing's own
recorded dispatch (I9), is checked against that dispatch's recorded set. It is no longer checked against a set computed
now from sites that predate the group's new files, which is what stopped PB3334's engine branch on four engine files at
2026-10-18 15:28. A consulted group recorded with no files is UNKNOWN and gets a warning that names it, not a stop. The
stop now names the arm its set came from instead of "a member_index hole". Self-test cases PB3263a-d pin the 15:28
shape.

Design: `DESIGN-orchestrator-loop.md` section 9 item 1b and 9.1. Tests: `test_plan_wave.py` 278/278 (section 12 and
the `sites_problems` cases in section 10) and `landing_check.py --self-test` 45/45. The tier fixture of section 9b now
gives each note its own area, because two of its notes declare no sites and a shared area would chain them all.

**Self-review.** Architecture: one meeting rule (`collision`), one formatter (`file_set_collisions`) and one reader of
a note's sites (`note_sites`, now with `declared`). Area strings are not normalized (`runtime/io` against `io`), so the
subsystem key is coarse, but the backfill and the new check make it a fallback rather than the main path. Performance:
`subsystems` runs per group per in-flight owner, a few thousand set unions. Duplication: none added. No codes used
(COBOLNET4619-4621 returned).

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261019T223933Z-65cfbe`; the self-tests of `test_plan_wave.py` and `landing_check.py` re-ran GREEN in it).

