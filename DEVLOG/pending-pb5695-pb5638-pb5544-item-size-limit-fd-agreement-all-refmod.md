## PB5695, PB5638, PB5544: one limit on the size of a data item, the 1985 every-entry FD rules, and the ALL walk's reference modifier

Three 1985 refusal defects, fixed in one branch.

- **PB5695 (crashes).** `01 T. 05 E PIC X(1000) OCCURS 2000000000.` compiled clean at every edition. `RecordLayout.ImageWidth`
  summed `int` products, so the size wrapped (the emitted code stored into -1,454,759,936 positions) and the program committed
  memory until the host ran out. No edition bounds the size of a data item, and §2023:4.5 admits the limits of an
  implementation. The compiler already held an elementary item to 2^27 character positions (COBOL2148, PB531). That constant
  moves to `RecordLayout.MaxCharacterPositions` as the one limit on one data item, elementary or group (determination
  D-ITEM-SIZE). `RecordLayout.ExceedsSizeLimit` is the one decider over a laid-out item. The new `CheckStorageLimitsPass`
  reports **COBOL7641** at the outermost item past the limit, in every section and at every edition. `ImageWidth` and
  `PhysicalWidth` sum in `long` and saturate.
- **PB5638 (under-rejects).** §1985:VII.3.7.4 GR11a: every entry of an external file connector has a LINAGE clause if any
  entry has one. An entry with no LINAGE used to be skipped. `FileDescriptionIdentity.Rule.AbsentCite` now reports such an
  entry (COBOL6847). The sweep found two more gaps. GR11c: a LINAGE data-name is now compared by its external data item
  identity; before, it was never compared at compile time. §1985:XIII.3.3.4 GR3: every entry describes a report file; this
  rule was never checked, and it is now a row. Part 2, the separately compiled describer, is filed as PB5912.
- **PB5544 (misleads diagnosis).** The ALL subscript is in the 1985 edition too (Amendment 1, §1985:IV.4.3.8.2.3 SR7). At 85
  and 2023, `FUNCTION MAX (TE (ALL) OF TT (2:2))` drew COBOL2363 beside the correct COBOL2776, and at 85 two cascading errors
  as well. `TryBindAllArgument` now uses one rule for every written subscript group and reference modifier, so the spelling
  draws COBOL2776 alone.

Cases: `DataItemSizeLimitTests` and the extended `AllSubscriptQualifierAfterSubscriptTests` (unit); five PB5638 cases under
`VII.3.7@1985-LINAGE-CLAUSE` and `XIII.3.3@1985-REPORT-CLAUSE`; `wiseowl/cases/_unassigned/pb5695-group-beyond-size-limit`.
A differential `check-batch` over 20,717 (source, edition) entries against a main build changed only the probes' verdicts:
every NIST program at 85 and every conformance case at 85 and 2023 kept its verdict. A lead filed on the way: PB5913, a
PICTURE of 64 M positions costs about 30 s of compile time, on main as well.
