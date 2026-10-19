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
