## PB5695, PB5638, PB5544: one limit on the size of a data item, the 1985 every-entry FD rules, and the ALL walk's reference modifier

Three 1985 refusal defects, fixed in one branch.

- **PB5695 (crashes).** `01 T. 05 E PIC X(1000) OCCURS 2000000000.` compiled clean at every edition. Its size wrapped in the
  compiler's 32-bit model, and the program committed memory until the host ran out. No edition bounds the size of a data
  item, and §2023:4.5 admits the limits of an implementation. The 2^27 limit COBOL2148 already applied to a PICTURE now
  covers any data item (determination D-ITEM-SIZE). It covers three measures: the image, at most 2^27 positions; the
  storage including overlays, at most 2^27 bytes; and the count of elementary items, at most 2^27.
  `RecordLayout.ExceedsSizeLimit` is the one decider. `CheckStorageLimitsPass` reports **COBOL7641** and then stops the
  compile (`DataDivisionRefused`).
  The review's R1-1 found the root still open: `DataItem.ByteWidth` summed `int` products with a checked `Sum`, so an
  over-limit group under a REDEFINES crashed the compiler. Five more sites kept the same shape.
  The fix is to the whole class of sites:
  - **One computation:** every size, OCCURS product, group sum, REDEFINES comparison, offset and bit extent is computed in
    64 bits in `RecordLayout`.
  - **One narrowing:** `RecordLayout.ItemSize` raises an internal compiler error above the largest admitted measure.
  - **Enforced:** `SizeArithmeticDriftTests`, a text-level test, bans the old shapes elsewhere.
  Round 2 (R2-2) found that the narrowing (then `Narrow`, now `ItemSize`) was also reached by the 2023 READ's search over
  HYPOTHETICAL counts. A legal bit + OCCURS DEPENDING record under RECORD VARYING TO 500000000 then crashed the compiler.
  `ItemSize` now narrows an item's own size only; a hypothetical count stays in 64 bits (`BitLayout.TableBitsAtCount`).
  The local-name products R2-3 listed now go through `RecordLayout`, and the test bans them.
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
`VII.3.7@1985-LINAGE-CLAUSE` and `XIII.3.3@1985-REPORT-CLAUSE`; `wiseowl/cases/_unassigned/pb5695-group-beyond-size-limit` and six more refusal cases there for R1-1 (a REDEFINES over the group,
an FD with a second record, OCCURS DEPENDING, a RENAMES range, a SORT record, LINKAGE). An emitted-C# differential over 401 suite
programs at 85 and 2023 against main shows no change.
A differential `check-batch` over 21,102 (source, edition) entries against a main build changed only the probes' verdicts:
every NIST program at 85 and every conformance case at 85 and 2023 kept its verdict. A lead filed on the way: PB5913, a
PICTURE of 64 M positions costs about 30 s of compile time, on main as well.
