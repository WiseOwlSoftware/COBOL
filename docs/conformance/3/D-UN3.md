- **D-UN3 — which side of UNSTRING's all-national rule a NUMERIC receiver stands on** (2026-09-22; kb/Work
  PB980, row `SR-14.9.48.3-3`). §2023:14.9.48.3 SR3 is category-worded: *"If any of identifier-1, identifier-2,
  identifier-3, identifier-4, identifier-5, literal-1, or literal-2 are of category national, then all shall be of
  category national."* SR4 then admits identifier-4 *"as usage display and category alphabetic, alphanumeric, or
  numeric; or as usage national and category national or numeric"*. A numeric receiver is never category national,
  so SR3 read by category alone would forbid every national sender from reaching SR4's usage-national numeric
  receiver, leaving that arm of SR4 unusable — no reading of the two rules together supports that. **WiseOwl COBOL
  reads SR4's own pairing into SR3: a usage-national numeric identifier-4 stands with the national operands, a
  usage-display numeric identifier-4 with the others**; every other operand answers by its §2023:8.5.2.1 class through
  `IntrinsicArgumentRules.ClassOf`, and a figurative delimiter (GR7: it takes identifier-1's class) never mixes.
  **Rejected readings:** category alone (above), and exempting numeric receivers entirely, which would admit a
  usage-display numeric receiver beside a national sender where SR4's pairing says otherwise. Implemented in
  `StringUnstringBinder.BindUnstring` over the shared `AllOrNothingClass` predicate (COBOL2306); witnessed by
  `conformance:cases/14.9.48-UNSTRING/pb979_unstring_any_length_national` (national sender into two usage-national numeric receivers)
  and `conformance:cases/14.9.48-UNSTRING/pb980-unstring-national-mix`.
