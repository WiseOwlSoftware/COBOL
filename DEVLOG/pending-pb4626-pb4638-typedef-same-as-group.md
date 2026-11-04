## A TYPEDEF entry's own SAME AS is expanded with the declaration, and PB4626 is discharged as a duplicate of PB4602 (PB4626, PB4638)

Wave 20261868 group D continued the PB4610/PB4611/PB4612 branch (DEVLOG 3112) in `DataBinder.cs`.

**PB4638.** `01 T TYPEDEF SAME AS Z.` over a group Z is a refused entry (ISO §13.16.3 SR12 lists no TYPEDEF beside SAME AS), and the PB4602
recovery keeps both the TYPEDEF role and the SAME AS reference so references to the type draw nothing further. `ExpandTemplate` expanded the
declaration's TYPE clause and then every MEMBER's TYPE and SAME AS, but `DescendantsOf(template)` never visits the template itself, so the
declaration's own SAME AS was the one reference nothing expanded: `01 X TYPE T.` copied an empty T and each `ZA OF X` drew a false COBOL1639
beside the one COBOL1555. Re-probe before the fix: COBOL1555 then two COBOL1639 (plain and STRONG). `ExpandTemplate` now walks
`SelfAndDescendantsOf(template)` and expands each item's TYPE and then SAME AS, the order `ExpandTypesPass.Run` gives storage items (ISO §13.18.49.4
GR1); the special-cased TYPE-on-self line is gone, so the next reference kind a declaration may carry needs no new line. After: COBOL1555 alone.
Siblings measured clean: a SAME AS on a member of a declaration, an elementary target, `BASED SAME AS` storage, `TYPEDEF TYPE T ALIGNED`, a
declaration chained over the SAME AS declaration, and a SAME AS declaration used as a member's type.
Witnesses: `ReferenceCompositionRecoveryTests.SameAsGroupWithTypedef_SubordinatesOfTheTypeResolve` (three entries, exactly one diagnostic each) and
the golden refusal `conformance:cases/13.16-Data-description/pb4638-typedef-same-as-group-one-refusal` (refused at 2002, 2014, 2023).

**PB4626 discharged.** The lead (`01 X TYPE T ALIGNED.` drawing COBOL2150, COBOL1942 and COBOL0881) is the same lead PB4602 landed the fix for; the
clerk filed it twice from one report. On this build the named repro and the sibling co-clauses (SAME AS ALIGNED, USAGE COMP, BLANK WHEN ZERO,
JUSTIFIED, REDEFINES with TYPE) each draw exactly one diagnostic. The note is retired with the evidence.

No inventory row is claimed or closed. Self-review (architecture, full code, performance, duplication): the change replaces a special-cased line with
the walk already used for the members, so no second copy of the expansion order exists; the walk runs once per declaration at bind.
