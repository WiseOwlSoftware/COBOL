## Entry 2228 — 2026-10-18 16:27 PDT — PB1629: a fault of placement born at a TYPE or SAME AS reference is reported at the entry that wrote the clause, and a copied table's eighth subscript is refused

### PB1629: COBOLNET1526 and the placement rules

kb/Work PB1629 (wave 1126, group E, the PB3353 campaign): COBOLNET1526 (a dynamic-capacity table in the file section,
ISO 8.5.1.9.1 3)) was reported at the working-storage TYPEDEF or SAME AS line when the table reached an FD or SD record
through a TYPE or SAME AS clause, so the user was told to change a declaration that is legal.

**Re-probe.** On the branch's own build every route reproduced: TYPE on an FD record, TYPE on a group inside it, SAME AS on
a record, SAME AS on a group, a SORT-MERGE (SD) record. The in-place route reported at the table's own line and was already
right. The note's premise held and its scope was too small: `CloneItem` gives a copy its template member's `DeclaredAt` on
purpose (a rule the declaration breaks is reported where the declaration is), and every post-build rule about where an
entry is PLACED inherited it. The OCCURS DEPENDING ON placement rules (COBOLNET0851, 0852, 0853, 0854, 0855, 0856, 0857 and
the SR18/SR21 attribute rule in the same loop) reported a copied table at the declaration the same way, measured on the
same build with a TYPE and a SAME AS copy of one table.

**The fix is one definition.** `DataItem.ReferenceSite` is the nearest enclosing entry that is no copy: the subject of the
TYPE or SAME AS clause, or the item itself when the programmer wrote it. `DynamicResolve` (1526), `OdoResolve` (every rule
after SR16, which is a fact of the written phrase and stays at the declaration) and the group-VALUE screen (COBOLNET1702,
which asked the TYPE clause alone through `TypeAnchor`, so a SAME AS copy was still positioned at its source) all ask it.
The messages end with the subject the clause belongs to (`ViaReference`), so a member the user did not write is explained.
13.18.57.4 GR1 and 13.18.49.4 GR1 ("as though the data description ... had been coded in place") are the reason: the
fault is the reference's.

**A sibling the sweep found, fixed in the same change.** The seven-subscript limit (COBOLNET2646, 13.18.38.3 SR10) walked
the written-entry forest only. A one-table type placed under seven tables is an eighth dimension, and compiled clean. It now
walks the composition forest and reports the copy once, at the reference site; a copy whose declaration already reaches
eight is reported in the declaration only.

**Review round 1.** The reviewer found that anchoring every post-SR16 rule at the reference also put a SECOND copy of each
declaration fact (a non-integer counter, a counter or table nested inside the copied description, a table followed inside
it, a counter inside the table's range) at the legal reference, where main reported it once at the declaration; and that
the depth rule's skip followed `ClonedFrom` to the written definition, so a copy of a copy re-reported an eighth dimension
created one level up. Fix: `DataItem.IsCopiedUnder(site)`; a rule is skipped at the reference when every entry it involves
is copied under that one site (the declaration, itself screened, reports it), and REDEFINES (0855) only when the
declaration's own copy trips the same test, because a TYPE declaration forms no REDEFINES class. The depth skip asks the
immediate source. Witness: `FactOfTheDeclaration_IsNotReportedAgainAtALegalReference` (counts equal the declaration's with
one and two references).

**Goldens.** `ReferenceSitePositionTests` (every route above, the in-place arm that must stay put, the declaration line that
must not be named, COBOLNET1702 through TYPE, COBOLNET2646 through TYPE and SAME AS and its once-only counts), and four
conformance cases: `pb1629-type-dynamic-table-in-file-section`, `pb1629-same-as-dynamic-table-in-file-section`,
`pb1629-type-table-needs-eight-subscripts` (refused at all four editions, expect COBOLNET2646) and its control
`pb1629_type_table_at_seven_subscripts_is_legal` (accepted at 2023, which prints T=Z).

**Rows.** The note closes none: no inventory row tracks where a diagnostic is positioned (`closes_rows: []` with its reason).

**Self-review.** The architecture lens found the two arms of the same dispatch (the TYPE-only anchor in the group-VALUE screen)
and the written-forest gap in the depth rule; both are fixed. The duplication lens found `TypeAnchor`-or-self written where
`ReferenceSite` now answers. No wrapper or alias was left behind.

**Review round 2.** The ambiguity arm of the same rule: a counter that is ambiguous among members of the one copied description
(SR1, COBOLNET1639) was reported at the declaration and again at each legal TYPE / SAME AS reference, because
`UniqueOrReportAmbiguous` ran unconditionally at the placement. It now asks the same verdict silently when the table and every
candidate lie inside the copy (`ambiguityIsDeclarationFact`), so `--permissive` still binds the first-declared counter in the
clone. The KEY phrase (data-name-2) counts through the same verdict at the table's own position and already reports once;
the rows `PB1629DC`-`PB1629DF` of `FactOfTheDeclaration_IsNotReportedAgainAtALegalReference` pin both.

