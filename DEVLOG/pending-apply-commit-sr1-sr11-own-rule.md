## PB3557: APPLY COMMIT SR1 and SR11 are reported under their own rules, and a file-name is a declared name

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
declaration hides the container's name, §13.18.27 GR1-GR2): a GLOBAL file or record area is SR11, a container's file without a
GLOBAL clause is SR1; one COBOL4709 each. The descriptor now names SR1-SR11. No new code.

**Goldens.** The two PB3338 negatives now expect COBOL4709; new `pb3557_apply_commit_sr11_global_record_area`;
`ApplyCommitEnclosingFileTests` (SR11 file and record area, SR1, an undefined word unchanged, a local data item and an own file both
hiding the container's GLOBAL file); `NonDataNameReferenceTests` (a file-name at a statement and at FILE STATUS, and a contained
program's GLOBAL versus non-GLOBAL file). Inventory rows SR-12.4.6.3.3-1 and -11 stay CONFORMS with corrected notes and added witnesses.

**Found, not fixed.** A contained program's FILE STATUS, RELATIVE KEY or OCCURS DEPENDING ON naming a container's GLOBAL data item
is refused as undefined (COBOL1639 / COBOL0851), on main too: the GLOBAL data subtrees are inherited after the contained data
division binds (`BinderDriver.BindUnitData`, `InheritGlobalSubtree`) — PB3766's timing, for data items. Reported as a lead.
