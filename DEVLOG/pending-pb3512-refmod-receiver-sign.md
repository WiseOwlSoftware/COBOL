## PB3512: a reference-modified MOVE receiver no longer takes the sender's operational sign

**What.** A MOVE of a signed numeric sender into a reference-modified alphanumeric receiver stored the sender's raw
stored form, sign included: `MOVE` of a PIC S9(4) 2468 into `X5(1:5)` gave `246H`, a packed -1234 gave `-1234`, a SIGN
LEADING SEPARATE -1357 gave `-1357`, the literal -42 gave `-42`. The same MOVE into the whole item was right. Found by
the second overnight differential fuzz run (fz04443, fz00248). Re-probed on this worktree's build of aef934867: the
note's four wrong lines reproduced exactly, and so did three shapes it did not name — a JUSTIFIED RIGHT item's slice
(`*246Q*`), a national slice (`246Q *`) and a slice of a numeric item (`246Q 0`).

**Rule.** ISO §8.4.3.3.4 GR6: "The unique data item is considered to be an elementary data item without the JUSTIFIED
clause." So the slice takes §14.9.25.4 GR6 a)'s elementary move: "If the sending operand is described as being signed
numeric, the operational sign is not moved; if the operational sign occupies a separate character position, that
character is not moved and the size of the sending operand is considered to be one less than its actual size." Both
checked with `cite.py --check` (OK §8.4.3.3.4 6), OK §14.9.25.4 6)). The note labelled the alphanumeric-to-national
conversion "GR6 b)"; `cite.py` puts that text in GR6's lead paragraph, and the note is corrected.

**Mechanism and fix.** `MoveClassifier.Kind` opened with `if (target is RefModPlace) return MoveKind.RefModSlice;`,
before the sender's category was asked, and the emitter's `RefModSlice` arm wrote `OperandText.AsString(source)`, the
sender's stored text. The kind is deleted: no `MoveKind` names a place shape now. A slice is classified like any
elementary receiver, its category read through `RefModPlace.Category` by the new `MoveClassifier.ReceivingCategory`;
the place decides only the receiving half. `MoveEmitter.SliceStore` is the slice's elementary store (the run-time-length
splice, and §8.3.3.6.4 GR2's whole-slice figurative and ALL fill), and its sender is `SendingCharacters`, the one
spelling of GR6 a)'s characters that `ConvertSource`'s three character arms now call too. A group sender into a slice
is GR4's group move through `ReceivingStore.SliceStorageArea`. That fixed a sibling found on the way: a group into a
NATIONAL slice took the group's bytes as its characters (`*AB1K*`) while the whole national receiver decodes them
(design D-N8); both decode alike now, and `ReceivingStore.StorageBytes` is the one byte-width spelling both group arms
share. The implicit MOVEs (READ/RETURN INTO, INITIALIZE, CORRESPONDING, function RETURNING) go through the same
classifier and inherit the fix. STRING, UNSTRING, INSPECT and ACCEPT convert nothing, and a CALL or INVOKE RETURNING
into a numeric item's slice is §14.8.3.3 nonconforming rather than a MOVE.

**Goldens.** `conformance-suite/cases/14.9.25-MOVE/pb3512_signed_sender_refmod_receiver` (85 through 2023: display,
packed, leading and trailing separate signs, a literal, a JUSTIFIED item's slice, truncation) and
`pb3512_numeric_sender_national_slice` (2002 through 2023, compared through national relations because a national
item's device form is the implementor's). Every expected value is derived from the rules. The drift test
`RefModMoveClassificationDriftTests` pins three things: no place-shaped kind, `Kind` asking no place-shape question, and
one spelling of the GR6 a) sending characters. Batch `w1147c-PB3512-refmod-receiver-witnesses` adds the witnesses to
GR-14.9.25.4-6 and GR-8.4.3.3.4-6, both already CONFORMS (GAP 66 → 66).

**Gate.** The first gate was RED on one Unit pin, `BoundaryImageChannelTests` naming the old slice-store local `rmp`, now `slice`. The re-gate, run `20261022T175710Z-71aaae`, read `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`. Self-review found that stale pin, and one sibling: the group into a national slice, fixed above.

**Branch review, round 1.** The reviewer found the national-slice fix incomplete for a national or bit GROUP. §14.9.25.4
GR4 says "Bit group items and national group items are treated as elementary items in the MOVE statement", but every
usage reader in `ReceivingStore` switched on the raw `Pic`, which is null for a GROUP-USAGE NATIONAL or BIT group. So
`MOVE AG TO NG(1:2)` stored N"AB" where GR4 gives NX"41424344", a bit group's slice was not unpacked, and the whole
national group (`MOVE AG TO NW`, wrong on main as well) took the group's bytes as its characters. All five readers now
ask `DataItem.OperandPic`, the as-if picture of §13.18.29.4 GR1 b) and GR2 b). The reviewer also noted that no test
pinned the group-into-slice decode. The golden `pb3512_group_sender_national_bit_slice` (2002 through 2023) now covers
an elementary national slice, a national group's slice, a whole national group, an elementary bit slice and a bit
group's slice. `ReceivingStoreDriftTests` fails if a usage decision in the store reads raw `Pic` again. Batch
`w1147c-PB3512-review-r1-group-into-national` adds both as witnesses to GR-14.9.25.4-4, which stays CONFORMS. The
re-gate, run `20261022T181334Z-4eed94`, read `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`, with
the new golden passing at 2002, 2014 and 2023.
