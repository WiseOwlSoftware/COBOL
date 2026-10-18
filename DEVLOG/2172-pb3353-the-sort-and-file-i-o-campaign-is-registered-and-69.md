## Entry 2172 — 2026-10-17 20:22 PDT — PB3353: the SORT and file I/O campaign is registered, and 69 related notes are classified against PB3339 and the PB3330 engine

The owner, 2026-10-17 (about 17:45-18:10 PDT): "We should end with all the SORT changes & fixes plus all the file I/O
changes and fixes in main", "Won't our redesign obsolete some / all>? of those prior notes?" and "Let's address and fix as
appropriate all the related notes". Two redesigns had happened that day: PB3339 (landed, PR #201) keeps SORT and MERGE
records in bounded memory with sorted runs on temporary files, and PB3330 (designed on `design-pb3330-keyed-storage-20261017`,
not yet on main) replaces the whole-file RELATIVE and INDEXED store with a paged on-disk engine, groups PB3331-PB3338 and
PB3340, whose deletion and keep lists are DESIGN-runtime-library §2.2.1.13.

**The search.** All 826 open notes on origin/main 3f3e0514a, not just the operator's 47-row keyword pass: frontmatter
`area`, `title`, `tags` and `spec_refs` (§9.1, §12.4, §13.4, the file clauses of §13.18, and the I-O statements of §14.9:
CLOSE, COMMIT, DELETE, MERGE, OPEN, READ, RELEASE, RETURN, REWRITE, ROLLBACK, SORT, START, UNLOCK, WRITE), and the bodies'
code sites under `src/Cobol.Net.Runtime/IO/` and the binder's and emitters' I-O and sort files. About 150 candidates were
triaged, and each note classified was read and checked against main's code, never judged from its title. The operator's
pass was both too wide (the merge QUEUE, the orchestrator) and too narrow (it missed the report-file, traceability, test
and architecture notes). Left out: cross-cutting notes where I/O is one site among many (PB3238's ON/NOT ON phrases,
PB3289, PB2747, PB2724, PB3212, PB2743, PB1561), Report Writer layout (PB1003, PB1652, PB1746, PB2343), tree-wide
transcription sweeps (PB718, PB385, PB1822, PB903), console streams (PB3294) and tooling.

**The classes.**
- **Tagged, still real (57):** the engine keeps their code (the frame codec, the lock primitives and region map,
  PhysicalFileTable, the sequential connector, FileRegistry's statement wrapping and lock statuses, the compiler side), or
  they are about tests, diagnostics, citations, traceability or docs. Each gained `PB3353` in `cluster` and one dated line.
  They include the I/O architecture findings of the PB1754 review and the PB2119 census, since the owner asked for "all the
  file I/O changes".
- **Obsolete by PB3339 (none):** the one candidate, PB2777 (SORT/MERGE held every record in memory), has its capacity
  question answered by PB3339: `SortStore` holds no record collection (`SortFileTable.cs:14-20`) and `SortWork` bounds its
  chunk at `COBOLNET_SORT_MEMORY`. But the operator's reviewed branch `notes-pb2777-sort-after-pb3339-20261017` keeps it
  open for the performance baseline's SORT and MERGE rows with a peak-memory measure, so it is still real. It joins
  PB3353's `blocked_by` and is not edited here, so the two branches do not collide. A first cut of this registration
  closed it, and the plan dry run's unlanded-branch list showed the other branch, so the edit was reverted.
- **Replaced by the engine (3, not edited):** PB2222 (IndexedConnector.LastReadPrime) and PB2342 (the IndexedConnector
  KeyIndex extraction) go with PB3334, and PB1096 (the declined APPLY COMMIT grammar) with PB3338. They are for the operator
  to link on the design branch, as PB2129, PB2753 and PB2755 already are.
- **Already fixed (3):** PB685 (DOC-A.1-71 written and closed by PB829), PB719 (§14.9.10.4's rules renumbered by the R66
  repair 2f43b0961; `cite.py --check` answers `1)` and `7)`), PB861 (the tautological posture test rebuilt by the PB322
  landing 5437188b5; `FileLockPosture.For` no longer exists).
- **Never true (5, retired):** PB2221, PB2223 and PB2224, with their siblings PB2217 and PB2218. The constructors they
  would delete are written by emitted code (`ReportWriterEmitter.cs:463/492/496`, `RuntimeApi.cs:1531`,
  `SequentialIoEmitter.cs:714`, `StatementEmitter.cs:179`, `EcEmitter.cs:691-692`). This is the census miss PB2744 and
  PB2756 record, and those two notes are themselves duplicates, left for the architecture lane.

**Sequencing.** `blocked_by` now orders the members that change the same code first: PB2759 waits for PB707 and PB671 (the
same WriteShared/RewriteShared preflights and PhysicalFileTable lock methods), PB2749 for PB2759, PB2308 for both, and PB1253
for PB2510 and PB2556 (whose determinations rewrite DOC-A.1-159). PB674's `blocked: true` was stale and is lifted: the 2002
text it waited for is not needed, since R61's method dated the ASSIGN USING half (PB746). PB284 is the prerequisite of the
engine's commit groups: §9.1.16-§9.1.18 have no inventory rows at all, while PB3337 and PB3338 expect to close §9.1.18 rows.

**The lead.** kb/Work PB3353 (decision, process_only) carries the owner's words, the scope and the end state: every member
landed or closed with evidence, and the PB3330 engine landed. Its `blocked_by` names the 57 tagged members and PB2777, and
PB3330 joins it when the design branch lands. `work.py check` is clean, and `work.py next --cluster PB3353` lists the 57
members and the lead in order. The plan dry run (`plan_wave.py --cluster PB3353 --budget-points 5 --dry-run`) needed a
member index for this tree, so `member_index.py --current` recorded one into the coordination cache first (treeKey
6573141bf007, the same C# tree as origin/main 3f3e0514a). It then planned 8 groups for 1.76 points: PB762, PB1515, PB3314,
PB2849, PB674, PB996, PB1925 and PB2533.

**Branch review, round 1 (two findings, both register-only).** R1-1: PB2777's re-scoping branch landed (PR #204) with its
`cluster` still `["PB1754"]`, so `plan_wave.py --cluster PB3353` could never plan the one SORT/MERGE capacity note the lead
waits on. PB2777's `cluster` is now `["PB1754", "PB3353"]`; `work.py next --cluster PB3353` lists it and the plan dry run
(`--budget-points 40`) offers it as group A, a finisher on `scripts/arch/perf_baseline.py`. R1-2: the sequencing edge "PB3337
and PB3338 need PB284 first" lived only in PB284's closing paragraph and the registrar's report, not in the end state the
design branch's landing follows. PB3353's end state gains item 4: PB3337 gains `PB284` in `blocked_by` (PB3338 inherits it
through PB3337) and the check at that landing is `work.py next --cluster PB3330`. The notes cannot carry the edge on main,
because PB3337 and PB3338 exist only on the design branch.

Notes only: no change under `src/` or `tests/`.

