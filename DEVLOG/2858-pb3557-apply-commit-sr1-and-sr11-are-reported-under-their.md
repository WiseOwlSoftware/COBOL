## Entry 2858 — 2026-10-31 10:16 PDT — PB3557: APPLY COMMIT SR1 and SR11 are reported under their own rules, and a file-name is a declared name

kb/Work PB3557 (a review lead of PB3338). A contained program whose APPLY COMMIT clause named a file of a containing program
drew COBOL1639 "no declaration in this source element gives the name 'RELF'" — false for a GLOBAL file, which is visible by
§13.18.27.4 GR1 — while the COBOL4709 descriptor claimed an SR1 report no code path made, and SR11 (§12.4.6.3.3: "Where a file or
record area is declared with the GLOBAL clause, it may only be specified in the APPLY COMMIT clause of the same source element")
had no report at all.

**Re-probe (Debug build, 0/0).** Both PB3338 negatives (`pb3338_apply_commit_sr11_global_file`, `pb3338_apply_commit_sr1_file_of_another_element`)
drew COBOL1639. Two siblings on the way: a file-name written where a data item is required (`MOVE 1 TO F1`, `FILE STATUS IS F1`)
drew the same false "no declaration" wording at every site that asks `DataBinder.NonDataDeclarationOf`, and a GLOBAL record area of
a containing GLOBAL FD (SR11's second noun) named in the clause drew it too.

**Fix.** The name's kind was never asked. `NonDataDeclarationOf`, the one answer to what a name declared as no data item is
(PB1906), gained `NonDataDeclarationKind.FileName` (an own SELECT or a containing element's GLOBAL file). The containers' files
are recorded before the unit binds by the new `DataBinder.InheritFileNames` (the file twin of `InheritGlobalTypeDecls`), so the
data division's own resolutions can ask; the merge into `FilesByName` stays after the bind, so an FD still finds only the unit's
own SELECT. `ResolveApplyCommit` asks `ScreenEnclosingFileOperand` of every identifier-1 that resolves to no data item (a local
declaration hides the container's name, §8.4.6.2.1 3) a)): a GLOBAL file or record area is SR11, a container's file without a
GLOBAL clause is SR1; one COBOL4709 each. The descriptor now names SR1-SR11. No new code.

**Goldens.** The two PB3338 negatives now expect COBOL4709; new `pb3557_apply_commit_sr11_global_record_area`;
`ApplyCommitEnclosingFileTests` (SR11 file and record area, SR1, an undefined word unchanged, a local data item and an own file both
hiding the container's GLOBAL file); `NonDataNameReferenceTests` (a file-name at a statement and at FILE STATUS, and a contained
program's GLOBAL versus non-GLOBAL file). Inventory rows SR-12.4.6.3.3-1 and -11 stay CONFORMS with corrected notes and added witnesses.

**Found, not fixed.** A contained program's FILE STATUS, RELATIVE KEY or OCCURS DEPENDING ON naming a container's GLOBAL data item
is refused as undefined (COBOL1639 / COBOL0851), on main too: the GLOBAL data subtrees are inherited after the contained data
division binds (`BinderDriver.BindUnitData`, `InheritGlobalSubtree`) — PB3766's timing, for data items. Reported as a lead.

**Finisher (CONFLICT at the landing).** Main's PB4147 deleted `conformance-suite/wiseowl/diagnostics.json` while this branch
edited it. The merge takes the deletion and writes the branch's three entries (the two PB3338 negatives now COBOL4709, the new
record-area golden) as `wiseowl/diagnostics/<id>.json`. The finisher also answered the review's three leads. A container's GLOBAL
record area written with its file-report-qualifier (`RREC IN RELF`, §8.4.2.2.2 Format 1) still drew COBOL1639, because the screen
returned at any qualifier. It now meets SR11 like the bare word. New golden
`pb3557_apply_commit_sr11_qualified_global_record_area`. The local-hides rule is now cited as §8.4.6.2.1 3) a)-b), which
replaces the inherited §13.18.27 GR1-GR2, here and in `BinderDriver.BindUnitData`. The COBOL4709 descriptor names SR11's record area.

**Second finisher (CONFLICT, and a collision with PB3766).** Main's PB4148 split the traceability inventory into one file per
row, and `carry_retired_registers.py` carried the branch's two rows. Main's PB3766 inherits a container's GLOBAL data roots into
the contained unit's name index, with their depth, before the APPLY COMMIT clause resolves. The screen had read "the lookup found
it" as "declared here", so on the merged head a container's GLOBAL record area would have drawn SR3. It now decides by the
DECLARING element (§8.4.6.2.1 3) a)-b)). It compares the depth of the nearest data items, of a report entry and of a GLOBAL file
(the new `InheritedFileDepth`), and the nearest is the referenced item. A contained program's own report entry therefore outranks
the container's GLOBAL record and draws COBOL5092 (§13.8.6.2.3). `InheritedGlobalRecordAreas` now maps the record itself to its
file, and the data-name lookup chooses the record: two GLOBAL files' records of one name are told apart by the file qualifier, and
the bare word is an ambiguity. The second name matcher, `InheritedGlobalRecordAreaOwner`, is gone. A sibling PB3766 left was fixed
on the way: the qualifier walk's file-name arm read only `FilesByName`, which gains a container's GLOBAL files after the unit
binds, so a contained program's own `OCCURS ... DEPENDING ON CNT IN RREC IN RELF` was refused (COBOL0851, legal source rejected);
the walk now asks `TryGetVisibleFile`. New golden `13.18.27-GLOBAL/pb3557-contained-clause-qualifies-by-global-file` and three
`ApplyCommitEnclosingFileTests` cases.

