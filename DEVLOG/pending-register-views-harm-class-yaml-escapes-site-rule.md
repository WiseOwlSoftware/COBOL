## The work register's views are subcommands, a misleading message has its own harm class, the reader decodes YAML escapes, and R3's member rule is checked

Wave 20261752 group A, four notes of the PB1957 tooling campaign, one root: `scripts/spec/work.py`, the register's one reader.

**PB3812, the reader (landed first).** `parse_frontmatter` read a one-line scalar with `strip('"')`, so a double-quoted title kept its
escapes: 279 titles on this tree read with a backslash in them (the note counted 236 when it was filed). A new `_scalar` decodes YAML
1.2 section 5.7's escapes (`\"`, `\\`, the control escapes, `\xNN \uNNNN \UNNNNNNNN`) and the doubled quote of a single-quoted value;
a backslash YAML does not define is the error code `unknown-escape:<key>` and an opening quote never closed is
`unterminated-quote:<key>`, both reported by `check` (`frontmatter_errors`, which now also owns the unterminated-list message
`closes_rows_shape` used to print) and never read as a silent pass. The first run found four real notes (PB1564, PB2482, PB2483, PB897)
whose Windows-path titles YAML itself would refuse; they now double their backslashes. Measured on `kb/Work`: titles showing an
escape artifact, 279 before and 0 after (17 still show a single backslash because the title names a Windows path).
`work-frontmatter-cases.json` gained a `scalars` section (32 cases, one per escape the table defines) and the self-test refuses a
fixture that leaves a key of `_YAML_ESCAPES` without a case. The other readers of register frontmatter (`census.py`,
`file_census_notes.py`, `clusters.py`, `backfill_closes_rows.py`, `gen_ledger.py`) already call `parse_frontmatter`.

**PB3799, the views.** `kb/Work.base`, `kb/Conformance Burn-down.base` and the three `.obsidian/` files are deleted, with
`check_base_agrees` (no shim): the owner does not open Obsidian, no agent reads a Bases view, and the Base was a second copy of the
harm predicate and of `waiting_on` in another language that a drift check had to hold equal. The views a reader needs are
`work.py blocked`, `unharmed` (open, not an upstream record, none of `HARM_FLAGS`: the exact complement of `next`'s harm test),
`analyses`, `landed` (with the rows each closed), `upstream` and `stats`, each with `--json` and `--top N`; they are rows of `VIEWS`
(a predicate and a sort key) over `HARM_LABELS` (one table of harm flags, `HARM_FLAGS` its keys) and `_is_open` (the one test for
"not done"), so a new terminal status or harm class reaches every view with no edit. `view_findings` states what each view lists
over a scratch register of one note per shape and one per harm flag. `inert_lane.CLASSIFICATION`, four C# tree scopes
(`LandingPipelineDriftTests`, `LandingLaneDriftTests`, `LandingMeasureDriftTests`) and every sentence naming the Base or the vault
in CLAUDE.md, three skills, DOC_INDEX, the plan, the design docs and the `kb/` pages now name the subcommand; `.gitignore` keeps
a guard against an application creating its state in the checkout. `gen-grammar-diagrams.ps1` no longer stamps the CSS class the
deleted snippet styled.

**PB3836, the harm class.** `misleads_diagnosis` is the fifth class of `HARM_LABELS`, ranked last, and `next` now ranks within a
severity by the harm's rank (it ranked by severity and id alone). `check` refuses `process_only: true` on a not-yet-done note whose
every site is under `src/` unless `process_only_reason:` says why or its `**Wave kind:**` is a restructuring (the architecture
review's behavior-neutral waves): it first refused 289 notes, 233 of them R2 restructures that pass by kind, and the other 56 were
re-read one by one. 28 got the flag and `process_only: false` (a message that names the wrong cause, quotes the wrong text, adds a
false second diagnostic or names no kind); 28 keep `process_only` with a stated reason (comment drift, duplication, a test for
behavior already right, two performance leads). PB3831, PB3832 and PB906 (landed) carry the flag for the record; PB911 carries it and
appears in `work.py next`, which lists 41 notes where it listed 13. The harm words outside `work.py` derive from one table
(`FINDING_HARMS`: the R2 collector and census writer, `fuzz_triage`), `.agent-fleet.json` weighs exactly `HARM_FLAGS` in rank order
(16, 8, 4, 2, 1), and `--self-test` fails when a template restates the classes and leaves one out.

**PB3797, R3's member rule.** A held note (cluster from PB3443 on, or a running campaign's) that names a code file with neither
members nor `` `path` (whole: why) `` is refused, and so is a member the file does not declare; a file the change creates needs
neither. 61 held notes failed it (180 bare mentions); the register is clean after the notes were brought to the rule: members from
line numbers (`source_units.names_at`), the 52 site lines of eight R2 notes rewritten with code spans, `(whole: <reason>)` for the
restructuring, backfilled and script-body sites, and stale names pointed at the modules that declare them now. The limits the
rewrite found are settled in the reader: a section number is read outside backticks (R3 says so), `.ps1`/`.psm1` have a unit reader
(`source_units.powershell_units`, all 50 tracked scripts read), `_MEMBER_NAME` accepts a PowerShell function's hyphen and the units
`<module>`, `<header>`, `<footer>`. MANDATORY-PRACTICES gains P15, the agent rule that no desktop GUI application is started on the
owner's screen. The 340 older open notes of idle clusters stay as PB3263 left them (refusing them would make the check a backlog).

**Found on the way.** The R2 writer's `(Type.Member)` parenthesis without code spans was read as prose, so eight R2 notes claimed
their files whole although they named members; the reader stays strict about the one form and the notes were rewritten.
`test_work.py` (named in two Sites paragraphs) never existed: the reader's test is `work.py --self-test`.

**Review round 1.** One blocking finding: `gen_ledger.HARM_WORDS` was a second hand-kept flag-to-label table beside
`work.HARM_LABELS`, so the next harm class would reach `next`, the views and the writers but not the ledger page. The
table is deleted; `gen_ledger.harm_text` walks `work.HARM_LABELS` and `silent`, and `gen_ledger.py --self-test` plants a
count per class and requires each on the page. Merging main brought PB4053, whose Sites paragraph named
`check_practices.py` for a member (`inflight.collision`) that lives in `scripts/orchestrator/inflight.py`; `work.py check`
refused it under the new member rule, so the paragraph now names the real files and members. The three LEAD findings
(`_scalar` interior quotes, the remaining Obsidian wording, the U+2032 docstring) stay for the clerk.

**Landing stop.** The landing's merge of main brought three notes filed after the branch's base that the two new rules
refuse: PB4073 and PB4074 set `process_only` on compiler sites, and PB4068 named its test file bare. Each was judged from
its own code site. PB4073 changes only a doc comment, and its one caller reports the bag, not the null. PB4074 hardens a
type, and every production construction already passes the compilation's bag. Both now say so in
`process_only_reason:`. PB4068 names its test method, and its stale line about PB3819's unlanded reader is gone.
