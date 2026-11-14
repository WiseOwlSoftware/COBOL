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

Gate: run 000002-20261114T164614Z at db6e01c2a9, `=== BUILD-LOCAL GATE: GREEN — Conformance 24,966/24,966 ran · Unit 40,766/40,766 ran · RepoDrift 177/177 ran · Characterization 37/37 ran (skipped 0) ===`. LINUX GATE: not part of the per-change gate (PB4327).

Branch review round 1 (two blocking findings), answered at the root. R1-1: the C# inventory drift test now skips a
document's location before its `File.Exists` (the conformance statement is a rendered view a fresh checkout lacks), but
`docchecks/inventory.py` judged only anchored document locations, so a BARE one (`DOC-A.1-75`'s `docs/FILES_DESIGN.md`)
was checked by nothing. The audit now judges every document location, a bare one for its file; probe: with
`docs/FILES_DESIGN.md` moved aside, `doc_audits.py --check` printed `DOC-A.1-75: code-location 'docs/FILES_DESIGN.md' — no
such file` and RED, and two self-test arms pin it. R1-2: rule (e) held every in-tree file view to a stamp only
`render_all` wrote, so running a generator directly (`drift_rules.py --out docs/DRIFT_RULES.md`, `gen-diagnostics-doc.ps1`)
left an unstamped view the gate called a hand edit. The stamp now has one writer, `rendered_views.write_view`: the Python
generators' `--out` reaches it through `write_rendered` (stamped at a view's path, the body alone elsewhere, so the
package's copy stays plain), the diagnostics test writes its raw render to the file `WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC`
names and the script places it with `rendered_views.py diagnostics --write`, and `render_all` fails a generator that exits
0 without a stamped view. Probe: the three generators run directly, then `rendered_views.py --check`: `rendered views OK —
7 views … none edited after its render`; the self-test grew four arms (20/20). The first fix gate (run
000001-20261114T171525Z) was RED at the INERT LANE audit: the C# test no longer names `docs/DIAGNOSTICS.md`, so its Markdown
allowance in `source_reads.py` was dead and went. Gate: run 000002-20261114T171657Z at 5336079ba2, `=== BUILD-LOCAL GATE:
GREEN — Conformance up to date … · Unit 40,766/40,766 ran · RepoDrift 177/177 ran · Characterization up to date … ===`.
