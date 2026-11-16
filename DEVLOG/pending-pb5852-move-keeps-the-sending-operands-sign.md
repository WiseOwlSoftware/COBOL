## At 1985 and 2002 a MOVE keeps the sending operand's sign over a zero result (kb/Work PB5852)

**The defect.** At `--std 85`, `MOVE -100 TO PIC S99 SIGN LEADING SEPARATE` stored `+00`. §1985:VI.6.19.4 GR4b1 says "the
sign of the sending operand is placed in the receiving item", so `-00` is due. The re-probe on this tree printed `+00`,
`+00`, `00+`, `0{` and `-01` at both 85 and 2023, as the note recorded. The root: the numeric store took the sign from the
truncated value at every edition, and a native carrier holds a value, which has no negative zero.

**The edition boundary.** §2002:14.8.24.3 GR4b1 has the 1985 sentence word for word. §2014:14.9.24.3 GR6d2a and
§2023:14.9.25.4 GR6d.2.a represent "the sign of the numeric value". So the boundary is 2014:
`DialectBehavior.MoveSignIsTheValuesSign`, VCR Table 7 row 7.84. No 2002 case or record was written (2002 is frozen).

**The design (DESIGN-data-model §2.1.1, committed before the code).** It uses the storage forms the design already has.
(A) `MoveEmitter.ConvertSource` writes an image-carried signed DISPLAY receiver through `CobolNum.FormatImageSendersSign`.
The sender's sign is read once: a literal's at compile time, an image sender's from its characters (so a negative zero
moved on stays negative), and any other sender's from its value, captured by a pattern variable around the store.
(B) `StorageFormPass` promotes, at 1985 and 2002 only, the native signed DISPLAY receivers that a store can hand a negative
zero. `SendersSignStore.MayHandNegativeZero` decides membership over the new `NumericStores` fact set that
`UsageCollectionPass` collects from every `BoundMove` and INITIALIZE store. A store qualifies when its sender may be
negative and either can lose every non-zero digit or can itself hold a negative zero. The pass runs to a fixed point.
BINARY and PACKED-DECIMAL receivers keep the value's sign, the representation documented in D-EDIT-SIGN-ZERO.
Arithmetic statements store a value (§1985:VI.6.8.4 GR2) and are unchanged. A literal `-0` and `VALUE -0` are unchanged,
because §1985:IV.4.2.2.2.2 does not decide whether `-0` is a negative operand.

**What it costs.** Compiled at `--std 85` before and after, the 1985 programs of the conformance corpus (1,985) and the
NIST programs (459) change 9 and 18 items from a native carrier to an image. Six of the 9 are in the new witness. All
other elementary fields stay native: 3,849 and 15,025 native fields before the change. The affected NIST programs and
conformance cases still match their goldens.

**Witnesses.** `VI.6.19@1985-MOVE-STATEMENT/pb5852_move_sign_of_sending_operand` (85) covers the note's five lines, a
negative zero moved on, low-order truncation, INITIALIZE REPLACING, a file round trip, an unsigned receiver and a positive
sender. It also checks that a negative zero compares equal to zero and is ZERO but neither NEGATIVE nor POSITIVE, and that
COMPUTE and ADD on it give `+00` and `+01`. The baseline compiler fails this case. `14.9.25-MOVE/pb5852_move_sign_of_numeric_value_2023`
holds the same program at 2023, with positive zeros. `NegativeZeroPromotionTests` covers an item in the set, items out of
it, the closure, and the empty set at 2023.

**Register.** `1985-GR-VI.6.19.4-4` keeps CONFORMS with its new witnesses. Its change record now says the sign changed in
2014. kb/Work PB5852 is landed. D-EDIT-SIGN-ZERO and DOC-A.1-85 now say which editions they speak for.
