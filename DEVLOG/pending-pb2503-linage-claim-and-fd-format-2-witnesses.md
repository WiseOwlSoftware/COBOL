## PB2503 - the LINAGE clause is a CLAIMED Annex A.4.11 element, and SR-13.4.5.3-7 cites the code and tests that enforce its first sentence

**What was open.** Two bookkeeping gaps the PB865 refuter found. (1) Inventory row `SR-13.4.5.3-7` named only
`MaterializeImpliedRecord` and the PB345 tests, which witness its second sentence (an indexed file needs a record description
entry); its first sentence, "Format 2 is the file description entry for a relative file or an indexed file", is enforced by
`FileDescriptionFormats.Of` and `DataBinder.FileDescriptionClauseAdmitted` and pinned by `pb1238-fd-linage-relative`, none of
which the row cited. (2) Annex A.4.11 item 10 lists "LINAGE clause (13.18.34)" with the Report Writer optional elements, and
section A.4.1 lets an implementation accept an optional element's syntax only when it claims support, so
`docs/CONFORMANCE.md` owed a statement of whether the project claims it.

**Re-probe (fresh `dotnet build Cobol.Net.sln -c Debug`).** Neither is a compiler defect. `pb1238-fd-linage-relative.cob` draws
COBOLNET2604 at `--std 85` and `2023` (a LINAGE clause is not a Format 2 clause), `pb1238-report-fd-linage.cob` draws COBOLNET2604
(Format 3 has no linage-clause), `pb345-indexed-no-record.cob` draws COBOLNET1837. A sequential file with `LINAGE IS 5 LINES WITH
FOOTING AT 4` runs at 85, 2002, 2014 and 2023 and `LINAGE-COUNTER` reads 2 after one write; the same file as LINE SEQUENTIAL is
refused COBOLNET0900 below 2023 for the organization, not for LINAGE.

**Fix.** No compiler code changed. (1) A witness-merge `record_verdicts` batch added the two code sites and the tests
`pb1238-fd-linage-relative` and `pb1238_fd_formats_admitted` to `SR-13.4.5.3-7`, with the notes restated to say where the first
sentence is enforced (verdict CONFORMS unchanged; GAP 66 to 66); both cases now list `SR-13.4.5.3-7` in their `rules`. (2)
`docs/CONFORMANCE.md` section 5's A.4.11 row now carries the determination: the LINAGE clause is CLAIMED and implemented whole
(A.4.1 makes the associated general rules, exception conditions and I-O statuses optional with it, so the claim covers EC-I-O-LINAGE
and the implementor-defined status `'90'`, not the clause alone), and it is bounded by the file description entry's own formats,
not by A.4.1's licence: a Format 2 or Format 3 entry that writes it is refused COBOLNET2604. The new drift fact
`FileDescriptionFormatDriftTests.TheLinageClaim_AgreesWithTheFileDescriptionFormatTable` reads that row and the format table, so
neither can change alone. Every citation went through `cite.py --check`; the one in the first draft that attributed GR26 to
section 13.18.34.4 (it is section 14.9.51.4 GR26) and the one that named SR4 for "a line sequential file is sequential" (the
rule is section 9.1.7.2) were corrected before commit.

**Rows.** None closed: `SR-13.4.5.3-7` was already CONFORMS; the A.4.11 determination is documentation no row carries.
`closes_rows: []` with its reason on the note.

**Sort and file I/O requirement.** The change reaches no runtime I/O, sort or locking code. The abnormal paths of the rule it
documents are pinned by the existing refusals (`pb1238-fd-linage-relative`, `pb1238-report-fd-linage`, `pb345-indexed-no-record`,
each with its diagnostic) and the LINAGE exception-path witnesses the claim now names (`pb526_linage_ec_checking`,
`pb686_linage_gr26_boundary`, `pb524-*`, `pb489-*`).

**Self-review.** The diff changes no compiler code, so the architecture, performance and duplication lenses had nothing to read;
the full-code lens checked that the new sentence says what the binder does (LINAGE row is `Sequential` only; Format 3 refused
by the same table) and the comment lens that no citation was inherited. The first gate was RED on
`HostWitnessTraitDriftTests` (the drift class now reads `docs/CONFORMANCE.md`, a top-level path, so it is a tree-wide sweep and
must carry `[Trait("Sweep", "tree")]`); fixed in the next commit.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261019T175916Z-cb709d`), with
`TheLinageClaim_AgreesWithTheFileDescriptionFormatTable` and the four editions of `pb1238-fd-linage-relative` Passed in leg 1.

**Branch review, round 1.** The reviewer found the new A.4.11 row cited the register row key `DOC-A.1-110` in prose, which
`AnnexA1RegisterDriftTests` forbids (prose cites "A.1 item N"; a key appears only as the key of its row). The gate had not
planned that test in leg 1 (it ran in the landing's population), so the leg-1 GREEN did not see it. Fixed: the sentence now reads
"(A.1 item 110)", and `python scripts/spec/audit_annex_a1.py --check` exits 0 with no findings. The planning gap is the open
kb/Work PB3247/PB3131 witness the reviewer named; it sits in `scripts/gate_plan.py`, not in this branch's files.
