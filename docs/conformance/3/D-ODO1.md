- **D-ODO1 — how many occurrences of an occurs-depending table an INITIALIZE statement initializes, when the
  VALUE phrase is what qualifies them** (2026-09-20; kb/Work PB577, row `GR-13.18.63.4-6`). Two rules answer and
  they do not agree. §2023:13.18.63.4 GR6 says *"the initialization of the associated data item behaves as if the
  value of the data item referenced by the DEPENDING phrase … is set to the maximum number of occurrences"*, and
  §2023:13.18.63.4 GR4c makes the execution of an INITIALIZE statement one of the two occasions a VALUE clause takes
  effect on — so read alone it would give the MAXIMUM. §2023:14.9.20.4 GR8 says *"For a variable-occurrence data item,
  the number of occurrences initialized is determined by the rules of the OCCURS clause for a receiving data
  item"*, which reaches §2023:13.18.38.4 GR8a — *"only that part of the table area that is specified by the value of
  the data item referenced by data-name-1 **at the start of the operation** will be used"* — so for a DEPENDING
  item OUTSIDE identifier-1 it gives the CURRENT value. (Inside identifier-1 the two agree: GR8b's *"If the group
  is a receiving operand, the maximum length of the group will be used"* is the maximum.) **WiseOwl COBOL follows
  §2023:14.9.20.4 GR8**: the INITIALIZE statement's own clause is the more specific rule for how many occurrences that
  statement initializes, and §2023:13.18.63.4 GR6's maximum-pretence governs the occasions on which no statement
  supplies a count — the initial-state seeding and the ALLOCATE seeding, where the DEPENDING item has no
  meaningful value yet and which is what GR6 exists to answer. **Rejected reading:** GR6's maximum-pretence
  overriding GR8 at the INITIALIZE occasion, which would make §2023:14.9.20.4 GR8's second sentence inert for every
  `TO VALUE` statement. The difference is observable only by raising the DEPENDING item afterwards and reading an
  occurrence the statement was told was outside the table. Witnessed at all three poles:
  `conformance:cases/13.18.63-VALUE/pb577_odo_value_initial_state` (the initial-state occasion — the maximum, GR6 as written),
  `conformance:cases/13.18.63-VALUE/pb577_odo_value_initialize` lines L1/L2 (the INITIALIZE occasion with the DEPENDING item
  outside — the current value) and L3 (inside — the maximum, plus GR6's last-sentence ordering).
