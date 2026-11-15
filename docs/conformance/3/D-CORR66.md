- **D-CORR66 — which data items are "in" a level-66 THROUGH alias for the CORRESPONDING phrase, and how they
  are qualified (§2023:14.7.6 rule 1 over §2023:13.18.45.4 GR2; kb/Work PB966).** Admission is not the latitude: §2023:14.9.25.3
  SR12 asks only that the MOVE CORRESPONDING operands "specify group data items" (`cite.py --check 14.9.25.3
  "Identifier-3 and identifier-4 shall specify group data items and shall not be reference-modified."` → OK,
  rule 12), and GR2 makes a THROUGH alias exactly that (`--check 13.18.45.4 "When the THROUGH phrase is
  specified, data-name-1 defines an alphanumeric group item that includes all elementary items"` → OK, rule 2),
  so the standard controls and the alias is admitted on either side. ADD and SUBTRACT CORRESPONDING still refuse
  it, and the no-THROUGH form too, because their SR6 says "shall not be described with level-number 66".
  **The latitude is rule 1's qualifiers.** Rule 1 pairs items with "the same data-name and the same qualifiers, if
  any, up to, but not including, D1 and D2" (`--check 14.7.6` → OK, rule 1), and the alias is in no included
  item's qualifier chain: those items are subordinate to the renamed record, not to the alias. **WiseOwl COBOL reads
  GR2's own words: the alias includes ELEMENTARY items and no groups, so each included item stands directly in the
  alias with no qualifier between.** It corresponds by data-name alone with an item at the first level of the
  other operand. A name the alias includes twice falls to rule 6, and rules 4 and 5 still exclude an included
  item under an OCCURS or REDEFINES entry inside the window, as they would under a group subordinate to D1. The
  rejected reading treats the record's intermediate groups inside the window as qualifiers, but GR2 does not
  include them in the alias. No model implementation offers a reading to follow: the owner's precedence (ISO,
  then GnuCOBOL, then IBM or Micro Focus) reaches them only where the standard is silent, and IBM Enterprise
  COBOL and Micro Focus are understood to refuse a level-66 CORRESPONDING operand outright. That was not measured
  here (the implementer host has no GnuCOBOL install). Implemented at `CorrespondingBinder.cs#CorrMembers` over
  `RenamesInfo.IncludedElementaryItems`, and pinned by `conformance:cases/14.7.6-CORRESPONDING/pb966_move_corresponding_through_alias`.
