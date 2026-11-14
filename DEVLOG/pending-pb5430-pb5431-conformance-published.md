## The conformance statement's pointers, its citation arm, and the statement in every package (PB5430 commit 3, PB5431)

Wave 20262031 continued the PB3726 group from wave 20262030's split: its 25 unlanded commits cherry-picked onto main
8b854e403f with no conflict (the merge of an already-landed branch left out), and the gate GREEN at that head before new
work (run 000001-20261114T162439Z: Conformance 24,966/24,966 · Unit 40,766/40,766 · RepoDrift 177/177 ·
Characterization 37/37 ran).

**PB5430, commit 3: the pointers and the arm.** Every instruction that names a place to write a determination now names
`docs/conformance/` (CLAUDE.md rule 1, the `spec-lookup` skill, the implementer dispatch spec's DOC-A.1 rule, the golden
lane and cloud adjudication briefs, the issue form, DOC_INDEX, the plan's publication criterion, DESIGN-test-build-ci
section 3.17.4, which now also states the views' stamp and rule (e)); README's links name the directory, and the session
probe renders the view. The CITATIONS audit gained its `CONFORMANCE` arm: every `CONFORMANCE.md §N` names a section
directory and every `CONFORMANCE.md#<key>` an entry key (`Conformance.keys`). Before it turned on, 23 citations did not
resolve: 16 were templates (`#DOC-A.1-<n>`) and 7 sat in kb/Work note bodies; the arm reads neither (a template is matched
possessively so its prefix is not read as a key; a filed note's prose is its owner's history, CLAUDE.md rule 8), and it
is GREEN with no baseline. Its self-test fires on the three misattribution shapes the burn-down rewrote and stays silent
on a real section, a real key, a template and a marked example. PB4053 (row-key partition of a keyed table) is retired:
with one file per row, `inflight.collision` finds no meeting between notes on different row files and still finds one on
the same row.

**PB5431: the published statement.** `Directory.Build.props` gains `CobolPackConformanceStatement`, joined to NuGet's
`TargetsForTfmSpecificContentInPackage`: every package renders the statement at pack time and carries it at
`docs/CONFORMANCE.md`, the path 62 compiler and CLI strings name. `dotnet pack` of the CLI and of the runtime: the packed
file's sha256 equals the checkout view's stamped body digest; a planted malformed entry fails the pack (MSB3073). The
manual has no build, so the half of the step that renders the statement beside it had nothing to join; the design
records the correction, and the manual's index says where the statement is.

Self-review: one renderer (the pack runs `conformance_doc.py`, the view's own generator); one reader of keys
(`Conformance.keys`); no second population for the arm (PHANTOM's loop). Found on the way and not fixed (outside the
group): the CLI packs as `cobol`, not the `WiseOwl.COBOL` tool package CLAUDE.md names (no `PackageId`/`PackAsTool`).

LINUX GATE: not part of the per-change gate (PB4327).
