## Review round 1 of listing-step-pb5949, fixed (2026-11-19)

- R1-1 (BLOCKING, a carried comment-entry header's entry uncoloured): fixed at its root. The conversion now records where a carried
  candidate header's comment-entry begins when it carries the line (`CommentEntryStart`, the one reading of that column, which the
  immediate fixed and free Header paths now use too; `CommentEntryHeader` no longer computes it). `ReferenceFormatMap.DiscardedAt`
  answers it for a `CommentEntryCandidate` line as "where the entry begins if the line heads a comment-entry paragraph", and
  `SyntaxSpanBuilder` reads it only of a draft `SourceSectionBuilder.SettleCandidates` settled as a header
  (`LineDraft.HeadsCarriedCommentEntry`), per listed copy of the line. Sibling sweep: the fixed CandidateHeader and the free-form
  carried header were both missing it; both fixed. A carried candidate that proves to be program text is pinned and converted again
  as ordinary lines (`CommentEntryPins`), so its inline comments are recorded by the ordinary path. Witness:
  `ListingSyntaxSpanTests.ACarriedCommentEntryCandidate_IsACommentEntryOnItsHeaderLine_AsAnImmediateOneIs` (the review's pd.cob
  shape), red on the pre-fix source ("Item not found"), green after.
- R1-2 (BLOCKING, a token past a supplementary-plane character cut short): fixed at its root. `PlaceOnLine` placed a stretch's end at
  its start POSITION plus its UTF-16 unit count, and a span that begins at the second unit of a surrogate pair started back at the
  pair's first unit, so the written-character guard dropped the rest. Each stretch now starts at the expanded index of its span's
  first unit (`SyntaxSpanBuilder.StretchStart`: the position's expanded index plus the units before it on the line that map to the same
  position) and runs one unit for one, and the stretches one token makes on one line are one piece. Sibling: a tab inside a literal
  (each space of its expansion after the first starts a span at the same position) is placed by the same rule. Sweep of the listing's
  other physical-to-expanded conversions (`ListingBuilder`, `DataMapBuilder`, `DiagnosticSectionBuilder`, `ObjectCodeMapBuilder`):
  each converts a single point, none a range, so none has the defect. Witness:
  `ListingSyntaxSpanTests.ALiteralHoldingASupplementaryPlaneCharacterOrATab_IsClassedWhole` (the review's pf.cob literals, the DISPLAY
  operand, the inline comment after the literal, and a tabbed literal), red on the pre-fix source, green after.
- Design doc: DESIGN-listing.md sections 4.4.1 (the carried header's record) and 7.2 "Placed through the one index" (the units and
  `StretchStart`) are corrected.
