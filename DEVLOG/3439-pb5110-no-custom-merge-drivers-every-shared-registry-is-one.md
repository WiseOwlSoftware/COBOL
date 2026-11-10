## Entry 3439 — 2026-11-10 10:12 PST — PB5110: no custom merge drivers; every shared registry is one file per entry

The owner decided on 2026-11-10 that no custom merge driver is needed: git merges non-conflicting changes to a shared file and
raises a conflict where there is one. The drivers (kb/Work PB3610) existed because two branches that each appended to one file
made git stop, and GitHub's merge queue, which runs plain git, could not run them (kb/Work PB3642), so a branch sharing a
driver-covered file with an open pull request waited. PB5109 had already deleted the hold; this change deletes the drivers and
gives each shared registry the shape that needs none.

**Re-probe.** The note asked for a design, not a repro. On this tree `scripts/merge_drivers.py` was 1,369 lines, the
`.gitattributes` block named four drivers, 308 constructs lived in one 4,002-line `constructs.json`, and every brief that merged
main said `python scripts/merge_drivers.py merge <ref> --keep`.

**What changed.**
- *Constructs.* `tests/version-matrix/constructs.json` is `tests/version-matrix/constructs/<id>.json`, 308 files, each
  `json.dumps(indent=1)`. One reader per language: `scripts/spec/construct_store.py` (Python: `edition_gates`, `vcr`,
  `audit_code_citations`, `gen_ledger`, the conformance generator), `TestRepo.ConstructRows()` (the C# tests) and
  `gen-constructs.ps1` (the generated registry, now in ordinal file-name order). The catalogue's description (the old
  `_comment`) lives in `construct_store.py`'s docstring.
- *Work notes.* A note is its file followed by `kb/Work/<id>/<yyyy-mm-dd>-<slug>.md` (`work.fold_text`, the one composition),
  so a paragraph added to a note after it is filed is a new file. `work.py` refuses a misnamed addendum, one that opens with
  frontmatter, a directory with no note, and a note file that is not its id. `prune_worktrees.register_path` counts an
  addendum as register-only.
- *Compaction.* `scripts/orchestrator/compact_notes.py [--apply]` folds a note's addenda back into the note only when no open
  pull request (each head fetched and diffed against its own merge base, never GitHub's 100-file list), worktree, local
  branch or dispatched group touches it; it folds nothing when the pull requests cannot be read. Its self-test plants an
  in-flight touch (nothing folds) and a quiet registry (folds) and checks the fold leaves the rendered note unchanged. The other
  registries are one file per entry already, so they have nothing to fold.
- *Drivers.* `scripts/merge_drivers.py` and the `.gitattributes` block are deleted. `scripts/git_merge.py` keeps what was never a
  driver: `merge` (plain `git merge --no-commit --no-ff`, then the commit; clean, conflicts or incomplete), `merge_conflicts`,
  `probe`, `foresee_queue` (the queue simulated) and `merged_tree` / `is_clean_merge`. Every caller changed in the same commit
  (enqueue, land_lane, prune_worktrees, landing_check, inflight, landing_rules, verdict_line, the briefs and skills). The
  planner's meeting rule no longer exempts any file. `abi_pin.py` lost `merge_text`, `unmeasured` and `needs_settle`; a pin
  conflict is the finisher's, resolved with `bump`, `repin` or `settle`. `landing_check.py` owns the public-API duplicate
  check, which matters more now: plain git keeps a symbol two branches added at different places twice (RS0025).
- *Guard.* `RegistryShapeDriftTests` refuses a `merge=` attribute and a registry file two entries share (construct, inventory
  and suite sidecars); a planted `merge=union` line turned it red.
- *Docs.* CLAUDE.md rule 8, DESIGN-test-build-ci.md section 3.17.2, DESIGN-orchestrator-loop.md sections 16.8 and 16.9, DOC_INDEX
  and the skills describe the current shape; every bare `constructs.json` in docs, comments and messages names the catalogue.

**Self-review.** Dead code found and removed with its callers: `verdict_line.conflict_files` (only the deleted
`conflict_report` read it). Left, and named in PB5110: the `merge-failed` BRANCH arm of `gate.NOT_RUN_CAUSES` and exit 5
have no producer now and are exercised only by a planted `Merged(incomplete=…)`; PB3738's keyed-table driver plan for the two
CONFORMANCE.md register tables is superseded.

**Review round 1 (R1-1, R1-2, R1-3).** R1-1 (blocking, fixed): the note shape (file plus addenda) was read only by `work.py`; the
planner's campaign texts, the landing check's `inflight.computed_file_set`, the member-index CLI and the fix lane's note view
(`clusters.ready_notes_view`, the one file `fix_clusters.py` reads) each read `<id>.md` alone. `work.note_text` is now the one
body reader and all four call it; self-test arms in `work.py` and `test_plan_wave.py` plant a Sites paragraph only an addendum
carries. The two public fleet tools `orient.py` and `fix_clusters.py` (submodule `tools/claude-skills`) still read one file per
note when run by hand; the planner's own path to `fix_clusters.py` is covered by the folded view. R1-2: the conflict brief no
longer prints `git merge <ref> --keep`, and its self-test refuses `--keep`. R1-3: the doubled "the construct catalogue" phrases
are corrected and the historical PHASE-13 review is restored to its original text.

