## PB3666: a boolean item receiving characters converts each one ('1' to one, every other to zero), so DISPLAY and the boolean operators agree

`MOVE "AB"` into `PIC 1(5)` (USAGE DISPLAY or USAGE BIT) stored the characters themselves: DISPLAY showed `AB000`
(`000AB` under JUSTIFIED) while `B-NOT` of the same item gave `11111`, because the operators read a position outside
`{'0','1'}` as zero. The re-probe on a fresh build reproduced it and found five more paths that deposit characters into
a boolean item without converting them: a national sender (`N"A1A"` gave `A1A00`), a reference-modified receiver
(`MOVE "AB1" TO BR (1:3)` gave `AB100`), STRING INTO a USAGE DISPLAY boolean item, a group sender into a DISPLAY boolean
item (which also space-filled the short tail where §14.6.8.6 wants zeros), and the element MOVE of a variable-length
group MOVE between corresponding dynamic-capacity tables (PB3598's sibling: `DISPLAY T1 (1)` printed `Z00`).

The standard leaves the conversion to the implementor. §14.6.8.6 transfers the data "after any necessary conversion"
and Table 16 admits an alphanumeric or national sender, but a boolean character is "the value zero or one" (§3.21) and
no rule converts any other character (§14.6.13.2 1) calls later references undefined). The owner approved the choice
the operators and ACCEPT already make (2026-10-24: "make DISPLAY and the boolean operators agree, and record the choice
in docs/CONFORMANCE.md"). It is determination D-B5 in `docs/CONFORMANCE.md` section 3: each transferred character
converts, `'1'` to boolean one and every other character to boolean zero, then the result aligns with zero fill or
truncation to the right (to the left under JUSTIFIED). GnuCOBOL 3.2 gives no precedent (USAGE BIT is unimplemented there
and its DISPLAY `PIC 1` moves text as a number); IBM has no boolean category.

Fix shape, one rule in one place. `CobolBool.FromCharacters` is the conversion and `CobolBool.Store` the fitting store
(convert, then `CobolString.Store` with the boolean-zero pad). `RuntimeApi.StrStoreBoolean`, the only emitter of a
boolean store, now emits `CobolBool.Store`, so every store into a boolean item (MOVE, COMPUTE, VALUE, group bit extents,
INITIALIZE REPLACING, READ INTO, MOVE CORRESPONDING, a CALL argument landed by §14.2.3 GR9) converts. The sites that fit
by another rule convert through the same two entry points: `PlaceRenderer.Write`'s reference-modified arm,
`StringEmitter.WriteImage`, and `ReceivingStore.Characters`, whose new boolean arm replaces the space-filling
`StrStoreAligned` for a GR4 group move into a USAGE DISPLAY boolean item. `AcceptSource.DeviceBoolean` now calls
`CobolBool.FromCharacters` instead of its private copy of the loop. One exemption, recorded in D-B5: a group sender into a
USAGE BIT item stays GR4's byte copy ("no conversion of data from one form of internal representation to another"), whose
bits are all valid booleans, so `MOVE GA TO BB` with GA holding `1A` stores the bits of x"31", `00110`. Not reached and
left as they are: VALUE (a boolean item takes only boolean literals, §13.18.63.3 SR10), UNSTRING (refuses a boolean
receiver, §14.9.48.3 SR4) and a .NET host's string landing, which refuses a character other than `0` or `1`.

Goldens (`conformance-suite/wiseowl/cases/14.9.25-MOVE/`): `pb3666_boolean_receiver_converts_characters` (2002 and up;
"AB", "1A0B1", "101", "B1", "1 1", a national sender, a reference-modified receiver, STRING, a group sender into both
usages, INITIALIZE REPLACING BOOLEAN DATA and MOVE CORRESPONDING, each under DISPLAY, BIT and both JUSTIFIED forms, with
B-NOT and B-AND on the results), `pb3666_dynamic_table_group_move` (2014 and up), and their refused-below-edition
negatives. Unit tests: `CobolBoolTests.Store_ConvertsEachCharacterThenFits` and the two `FromCharacters` tests. Drift pin:
`ReceivingStoreDriftTests.EveryStoreIntoABooleanItem_ConvertsItsCharacters`, which fails when a CodeGen file spells the
zero-padded boolean store itself or when one of the converting entry points is no longer spelled where its store reaches it.

`RuntimeAbi.pin`: the boundary-layout fixture's compiler-side hash moved (the callee's landing of a boolean formal now
spells `CobolBool.Store`), so the pin was re-measured with `abi_pin.py repin`, not bumped: the caller still sends the same
characters and a callee compiled before this change lands them as it did, so the call layout and the runtime's public
surface are unchanged, and a mixed pair differs only for a boolean formal given a character that is not `0` or `1`.
No inventory row is claimed or closed; the register note PB3666 is flipped to landed with `closes_rows: []` and its reason.
The change reaches no file I/O status, sort or declarative path (READ INTO is an implicit MOVE through the same store).

Review round 1 (wave 20261105, finisher): the new boolean arm of `ReceivingStore.Characters` caught a USAGE NATIONAL boolean receiver too, so a GR4 group move converted and zero-filled the UTF-16BE BYTE image before `StorageCharacters` decoded it (`MOVE GN TO BN` with GN holding N"101" displayed three CJK characters and B-NOT gave 111; main gave 101 and 010). The arm is now `Usage: Usage.Display` only (a BIT area is packed bytes, a national one UTF-16BE pairs; neither is characters), pinned by line L12 of the golden and by `ReceivingStoreDriftTests`. The two LEAD comment findings were fixed in the files already changed: the design invariant and D-B5 now name the two bypasses that leave undefined content (a group receiver under GR4, a REDEFINES view; section 14.6.13.2 1)), and the STRING comment and golden header no longer present GR3 a) as the authority for the conversion (it prescribes the alphanumeric move; D-B5 is the authority).
