## Entry 2204 — 2026-10-18 06:22 PDT — PB822: the design docs' SORT/MERGE sign determination is re-expressed on the V59 byte image, stated once, and pinned by MERGE witnesses

kb/Work PB822 (campaign PB3353) found that `PicInfo.ImageSignKind` is gone from `src` while four design-doc sites still
rested a determination on it: `COBOLNET_DESIGN.md` §8.2 and §14.4 (which contradicted each other), `COBOLNET_FILES_DESIGN.md`
D7, and `COBOLNET_OO_DESIGN.md` D6 item 1. The note refused to say which statement was current and asked for a
measurement of whether D7's "never `BinaryMinus`" property still holds.

**Re-probe.** `grep -rn ImageSignKind src/` is empty. A SORT over a signed `COMP` key and over a signed `COMP-3` key
(ascending and descending, values from -99999 to 99999) returns negatives first, in algebraic order. The property holds,
but not through the mechanism D7 names: since the V59 re-base a fixed-point leaf's image IS its bytes, `SortBinder` binds
the key LEAF (`BoundSortMergeKey.Item`), and `SortKeyColumns.NumericKey` decodes the byte window with
`CobolNum.ParseImage` on the leaf's own profile. No image sign form exists, so a descriptor carrying one has no referent.

**What was stale beyond the member name.** D7's title and rationale described the pre-V59 zoned `Pic.Digits`-wide image,
"80 chars where the legacy wrote 72+4", key geometry "in digit-image coordinates" and listed the legacy raw-byte form as the
REJECTED alternative; §14.4 already names the zoned form a retired loser. §8.2 also said float, COMP-5 and INDEX leaves stay
outside the codec, which PB164 and R40 superseded (`DataItem.IsImageCapable`); §14.4 carried the same sentence.

**Fix.** The SORT/MERGE key consequence is now stated ONCE, in §14.4's total rule: the key descriptor carries the leaf, the
byte window is decoded through the leaf's profile, the order is algebraic (§14.9.40.4 GR8; §14.9.24.4 GR3 a)/b), both via
§8.8.4.2.4), the MERGE sequence check (§14.9.24.4 GR6) judges on that same order, and the failure of any other decode is
silent (a byte-wise compare orders negative COMP keys after positives and packed keys by magnitude). §8.2 and D7 point at it and keep what they own; D7's title,
rationale and rejected alternatives are rewritten; the OO design's SIGN representation names `PicInfo.SignKind`.
Every citation was run through `cite.py --check` (the MERGE comparison rule is GR3, not the SORT's GR8 that the new tests'
first draft carried over).

**Witnesses.** End-to-end tests in `MixedUsageRecordImageDifferentialTests`, which had SORT keys only:
`Merge_SignedBinaryKey_Ascending_NegativesOrderAlgebraically`, `Merge_SignedPackedKey_Descending_HighestFirst` and the
abnormal path, EC-SORT-MERGE-SEQUENCE over a signed COMP key, in four programs (`Merge_SignedBinaryKey_SequenceCheck_*`):
ASCENDING and DESCENDING, each WITH and WITHOUT a USE declarative. In each, a USING file ordered by value whose bytes run
the other way raises nothing; one out of order by value raises the condition, the declarative (when present) runs and the
statement alone is terminated (its output procedure never runs, the run unit continues). Review round 1 found that the
first draft covered only the ascending, declarative arm, misstated the silent failure (a packed key orders by magnitude,
not 0xFF-first) and kept a stale GR ordinal in a header; all three were fixed. The class's stale "digit-image
representation" wording was corrected in the same file.

**Register.** PB822 flips to landed with `closes_rows: []` (no inventory row claimed it); PB868 item 1 is the same defect
and is noted there as repaired, its items 2-3 and the symbol-existence audit staying open.

**Gate.** See the report.

