**The adversarial review of the re-design, answered (2026-11-12; branch `design-pb3726`).** A read-only reviewer
attacked `docs/rearchitecture/DESIGN-orchestrator-loop.md` section 16 at `9c7c07652c` and returned SOUND WITH FIXES:
thirteen findings, six blocking (F1, F2, F3, F6, F8, F9). Each finding's command was re-run on the worktree before it
was acted on; every one reproduced. The design section was rewritten in place (it describes the design as it now
stands, CLAUDE.md rule 6); the notes changed as listed. Section numbers below are the design document's own.

- **F1 (high), reproduced** (`git grep -n -- "--self-test" -- tests`; `git grep -n AssertDeclares -- tests`): four
  compiler tests run or read script self-tests. Section 16.2 now names them, decides their fate (arms into
  `self_test_arms.DECLARED`, the C# run or read deleted by the step that converts the subject; `SelfTestArmSource.cs`
  and its test deleted by the last caller's step), and replaces the caller grep with one that finds them
  (`rg -n -e '--self-test' -e 'AssertDeclares' -e 'SelfTestArmSource' -e 'self_test\(' tests scripts .claude docs .github conformance-suite CLAUDE.md`),
  which PB5409 re-runs as its done-condition. Sites and done-conditions: PB5404 (`DerivedVerdictDriftTests.cs`), PB5405
  (`AnnexA2UndefinedListDriftTests.cs`), PB5408 (`ArchCensusDriftTests.cs`, `ArchReviewFleetDriftTests.cs`,
  `SelfTestArmSource.cs`, `SelfTestArmSourceTests.cs`).
- **F2 (high), reproduced** (125 top-level bullets in section 3 of the document, 75 keyed; prose after the entries of
  sections 2, 4, 5 and 7; 3.7.1 unlisted): the conformance storage step is split. PB5429 (new) makes the document regular
  in place (keys, order, row bodies, epilogues, 3.7.1, a `conformance_source.py --check`) with the readers as its oracle
  plus a line-multiset diff; PB5430 (new) is the cut, proven by the byte-identical render of the normalised file. The
  generated section 3 index was dropped (the monolith never had one, so nothing could prove it).
- **F3 (high), reproduced** (CLAUDE.md line 29; `dispatch-spec-implementer.md:65`; 62 product strings; `rendered_views.py`
  "ABSENT IS NEVER STALE"; no guard): the view stays untracked. Section 16.8 adds the stamp (every in-tree file view, by
  the registry), rule (e) NOT EDITED in `rendered_views.py --check` (a hand edit is RED and a render refuses to overwrite
  it), the full list of instructions that move to `docs/conformance/` (CLAUDE.md rule 1 included; MANDATORY-PRACTICES and
  `land-a-fix` do not name the file, so they left the list), the decision on the 62 strings (they name the published
  statement, unchanged) and its step, PB5431 (new: the package and the manual's build carry the render). The reviewer's
  case for a tracked render is in section 16.8 with the reason it was not taken. One decision differs from the brief: a
  STALE unedited view is not RED (see the last line).
- **F4 (medium), reproduced** (11 citations of the standard's 4.2.16 as a section of the document, 4 of a section 7.3,
  and the unresolvable anchors `#4.2.16`, `#7`, `#A.4.2`, `#A.4.9`): the check is now an arm of the CITATIONS audit (its
  population and its one exemption marker), and PB5430's first commit burns the debt down before the arm turns on, with
  no baseline. The inventory records and case tables under `tests/version-matrix/` and `tests/doc-facts/` are the
  inventory audit's population, not this arm's.
- **F5 (medium), reproduced** (`inert_lane.py` code_readers; `docchecks/design.py:90-98`; `inventory_view.py:443-446`):
  PB5395 now claims `inert_lane.py` (the CODE_READS entry for `conformance_source.py`, read by `generate.py`: decided over
  moving the report, section 16.8 step A), `docchecks/design.py` and `inventory_view.py`.
- **F6 (medium-high), reproduced** (`work.note_sites` overlap of three documents between the tier-3 pair): both answers.
  `REMAINING` is a SET, one path per line, so no two branches (in the campaign or out of it) collide on a count; and the
  chain is strictly serial, the case layout first, so every later step writes case files. Section 16.2 ("Why a set and
  not a count") and section 16.10 give the reasons.
- **F7 (medium), reproduced** (`work.py` `note_sites` unions every Sites block of a note and its dated files): no dated
  file can withdraw a claim, so every old step note whose body was stale was RETIRED (frontmatter only: `status`, `tier`,
  `blocked_by`, `closes_rows_reason` naming the successor) and filed fresh; their dated addenda (never on main) were
  removed. Retired: PB3729, PB3730, PB3731, PB3732, PB3733, PB3734, PB3735, PB3736, PB3737, PB3738. This also disposes of
  the wrongly converted standard-form citations in their bodies. PB4053 (blocked by PB3738) now waits on PB5430, which
  retires it with its acceptance run as evidence.
- **F8 (medium-high), reproduced** (`scratch_repo.py:319-345`; `subject(__file__)` invisible to the static closure):
  section 16.2 now designs the import (the runner sets `PYTHONPATH` to `scripts/` for every `selftests/` file, one place;
  `self_tests.py --only` for a hand run; a bare run fails loudly), the two static edges (`import selftest_cases` and the
  `subject(__file__)` call, resolved through `selftest_layout.subject_of`) and `scratch_repo`'s reach (every `selftests/`
  file is whole self-test scope). PB5428 carries a planted case for each.
- **F9 (medium), reproduced** (eight `scripts/docchecks/*.py` delegates, run only through `doc_audits.py`): the
  `REMAINING` set counts a delegate `self_test*`/`*_arms` another self-test calls, the label oracle walks delegates and
  treats an empty set as a failure (section 16.1), and PB5401 claims `scripts/docchecks/*.py`.
- **F10 (medium), reproduced** (`perf_baseline.py:847`): `FakeHost` becomes `scripts/gate_fake_host.py`, an ordinary
  shared module placed by the step that first moves it (PB5432, which re-points `perf_baseline.py`); section 16.2 states
  the rule for every shared double.
- **F11 (low-medium), reproduced and extended**: an AST walk found seven cross-boundary edges, not the reviewer's four
  (also `_generate` → `facets` and `_pool_listings`/`_run_hosts` → `_handed`). Section 16.5 decides each; the DAG line is
  now `gate_host ← gate_reuse, gate_generated ← gate_legs ← gate`. PB5411 carries it.
- **F12 (low), reproduced**: `NET_KNOWN` and `entry_point_arms`' four literals are named in section 16 (rule 3, section
  16.2, section 16.5), each owned by the step that moves its text (PB5432, PB5433, PB5400, PB5407, PB5441, PB5408).
- **F13 (low), reproduced**: the README lines are 10, 145, 191 and 224; the manual contradiction is resolved by one rule
  (a Markdown LINK moves to `docs/conformance/`, a text CITATION does not).
- **Step size:** PB5403 now covers `scripts/p*`–`s*` (about 1,750 self-test lines) and PB5440 (new) `t*`–`v*` (about
  1,900); PB5407 covers `scripts/orchestrator/[a-r]*` (about 2,850) and PB5441 (new) the rest (about 2,000).
- **RS0016:** not a blocker (the reviewer's judgement, kept): PB5396 and section 16.9 now say the audit is the authority
  and `--write` the fixer, and the open assumption is dropped.
- **Decided differently from the brief:** rule (e) refuses a view whose BODY was edited (its digest is not the stamp's)
  or that has no stamp, not every view that is "not the current render". A stale view (sources moved on, body untouched)
  loses nothing, and making it RED would turn gates red after every pull and train the reflex, re-rendering, that
  overwrites an edit; the render's refusal to overwrite an edited view closes that path instead.
