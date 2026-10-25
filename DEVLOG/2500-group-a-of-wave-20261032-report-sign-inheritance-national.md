## Entry 2500 — 2026-10-24 19:03 PDT — Group A of wave 20261032: report SIGN inheritance, national report numerics, SYNCHRONIZED as a described clause, the COBOL-85 SIGN subject, and unresolved prototype pointers

Seven kb/Work notes from one root, the data binder (PB493, PB3280, PB539, PB538, PB494, PB1319, PB3162). Every note was
re-probed on the branch's own build before any change, and every one reproduced.

**PB493 (wrong answer, silent).** A SIGN clause on a report group entry reached no printable item:
`01 DET-A TYPE DE LINE PLUS 1 SIGN LEADING SEPARATE. 02 COLUMN 1 PIC S9(3) SOURCE WN.` printed `00N` for -5, the
§13.18.52.4 GR4 default, where GR1 with GR6 a)/b) requires `-005`. The report entry's description handed the enclosing
USAGE down (§13.18.60.4 GR1) and no SIGN. `InheritedReportUsage` became `EnclosingReportItem`, the description carries
`InheritedSign`, and `ReportPrintablePicture` applies it through `DataBinder.WithInheritedSign`, the one rule that
`ApplyEffectiveSign` (the data division walk and the SAME AS transform) now delegates to as well. Determination D-SIGN1
(CONFORMANCE.md §3): an inherited clause reaches a display or national signed item; a binary or packed item keeps its
representation. Golden `2002/pb493_report_group_sign_inherited` (GR1, GR2, GR3 in the REPORT SECTION). Rows
GR-13.18.52.4-1/-2 re-verdicted CONFORMS.

**PB3280 (backend crash).** A numeric printable item of usage NATIONAL (`PIC 9(3) USAGE NATIONAL SOURCE N`) reached the
line as a long and Roslyn refused the generated C# (CS1503), because the printable item's image mark was the DISPLAY-only
shape. It now reads `PicInfo.IsCharacterFormNumeric`, the one image-form predicate; `DisplayUsageUnionDriftTests` drops
the Reports.cs entry and says why. Golden `2002/pb3280_report_national_numeric_item`.

**PB538 and PB539 (SYNCHRONIZED).** The clause's LEFT/RIGHT phrase was discarded into a bool, so declarations differing
only in `SYNC LEFT` and `SYNC RIGHT` were "equivalent" under §8.5.3.1 and a CALL between them compiled clean; and a
group's clause was never copied to its leaves (§13.18.55.4 GR1), so a TYPEDEF STRONG with SYNC on the group and the same
declaration with SYNC on each leaf were two types and a legal CALL was refused COBOLNET1688. `DataItem.Synchronized` is
now a `SynchronizedPhrase?` (Unqualified, Left, Right), compared and keyed by `StrongTypeModel`;
`DataBinder.InheritSynchronized` (new `DataBinder.SynchronizedClause.cs`) writes a group's clause onto each subordinate
elementary item for which it is permitted, stopping below a group-level VALUE (§13.18.63.3 SR14) and a CONSTANT RECORD
(§13.16.3 SR13). Templates take it when `ExpandTemplate` completes them, before any TYPE subject copies their members;
the storage forest after `ExpandTypes`. The §13.16.3 SR24 d) screen reads the members. Goldens
`2023/pb539_sync_group_as_if_leaves` and negative `pb538-sync-left-right-differ`. DOC-A.1-197's cell says the three forms
are three clauses. Rows GR-13.18.55.4-1/-4/-5 CONFORMS.

**PB494 (edition gate blind spot).** `01 TG TYPEDEF SYNC.` compiled at 2002 and 2014: the group-item gate (Annex E.3.2
item 6) sat in the storage forest's usage walk, which a type declaration is not part of. It moved to
`GateSynchronizedGroup`, asked by the GR1 walk of every group entry that WROTE the clause, and the stale witness header
of `sync_group_below_2023.cob` (it claimed --permissive made the refusal a warning) now matches CA14 option (a). Negative
`pb494-typedef-sync-group-below-2023`. Row SR-13.18.55.3-1 CONFORMS. The sibling sweep found the GROUP-USAGE screens in
the same walk share the blind spot (reported as a lead).

**PB1319 (COBOL-85 SIGN subject).** The 85 text (vendor transcriptions of ANSI X3.23-1985) admits a group SIGN only over at
least one signed numeric entry and requires the entries the clause applies to be USAGE DISPLAY; 2023 keeps neither. Two
construct rows introduced at 2002 (`sign-group-unsigned-subordinates-2002`, `sign-group-nondisplay-subordinate-2002`),
asked by `DataBinder.GateGroupSignSubordinates`; VCR Table 7 row 7.40 states the derived 2002 edge; CONFORMANCE.md
D-SIGN2. Golden `2002/pb1319_sign_group_subjects_2002`, negatives `pb1319-sign-group-unsigned-85` and
`pb1319-sign-group-comp-85`; row H left the 85 golden `pb537_sign_clause_subjects`. Every SIGN-bearing 85 source in the
suite, NIST, characterization and differential corpora compiled at 85: only that row was refused.

**PB3162 (SET and argument lanes disagreed).** For restricted program-pointers whose prototypes have no compile-time
signature, `SET PB TO PA` was admitted while passing PA BY REFERENCE or BY CONTENT to a formal of PB's type raised
EC-PROGRAM-ARG-MISMATCH. An unresolved prototype's identity is now marked (`PrototypeSignatures.ExternalIdentity`,
`ActivationDescription.ExternalRestrictionPrefix`), and the description lane compares pointer restrictions through one
helper, `ActivationRelations.SamePointerRestriction`, that lets such a pair conform as `PrototypeSignatures.Same` does.
Golden `2002/pb3162_external_prototype_pointer_set_and_pass` (with an unrestricted-pointer control that is still refused).

No diagnostic codes were allocated (COBOLNET4975-4977 returned).

