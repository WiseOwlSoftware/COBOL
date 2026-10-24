## Entry 2444 — 2026-10-24 00:20 PDT — The report counters' implicit description is GnuCOBOL's unsigned 32-bit binary integer (PB1746)

PAGE-COUNTER and LINE-COUNTER are "temporary unsigned integer data items of class and category numeric" (§8.4.3.15.4 GR1)
and the standard names neither a size nor a usage. R26 and PB1153/PB1199/PB1318 had answered with `PIC 9(18) USAGE
DISPLAY`, an implementor choice made without asking CLAUDE.md rule 1's next authority. The fix lane re-probed the note
(`DISPLAY PAGE-COUNTER` printed `000000000000000001`, `FUNCTION LENGTH` answered 18, `STRING` accepted the register) and
surveyed cobc 3.2.0 under WSL plus the pinned source: `build_report` (cobc/tree.c) declares both registers
`CB_USAGE_UNSIGNED_INT`, so `DISPLAY` prints `0000000001`, `MOVE` to `PIC X(4)` stores `0000`, `STRING` refuses the register,
`MOVE 5000000000` leaves `0705032704` and `ADD 1` at 4,294,967,295 leaves `0000000000`. LINAGE-COUNTER is `UNSIGNED-INT` there
too, but §8.4.3.14.4 GR1 sizes it by the LINAGE page size's digits, so the standard decides that one and it stays
`PIC 9(d) USAGE DISPLAY`.

**One statement of the description.** The report counters' description was written twice and disagreed with itself:
`PicInfo.ReportCounterItem` (the receiving profile, USAGE BINARY 18 digits) and `AlgebraicRanges.ReportCounterCapacity`
(`999999999999999999`, the DISPLAY image width). `ReportCounterItem` is now the unsigned `BinaryLong` shape (10 digits, 4
bytes, capacity 4,294,967,295; the `OCCURS DYNAMIC` CAPACITY register already had it) and `AlgebraicRanges.DescribeCounter`
derives every consumer's answer from it: the algebraic extremes and class conditions, the character image width, the new
`CounterRegisterByteLength` that FUNCTION LENGTH / BYTE-LENGTH fold (4, where they said 18) and the new usage predicate. The
runtime twin is `CobolReport.CounterModulus` (2^32): the engine's `LineCounter` / `PageCounter` setters reduce every
assignment, so a page advance past the capacity wraps to 0 as cobc's does. `ReportCounterDescriptionDriftTests` pins the
runtime modulus to the compiler's description (storage bits, capacity, digits, the reduction).

**The usage predicate drives three screens** (§14.9.43.3 SR1, §8.8.4.2.5): STRING refuses a PAGE-/LINE-COUNTER sender
(COBOLNET1626) and a relation with an alphanumeric operand refuses it (COBOLNET2532), as for any declared `BINARY` item;
LINAGE-COUNTER, a usage display item, keeps both. Found on the way and fixed in the same change (I9): UNSTRING's SR2
category screen (§14.9.48.3 SR2, "category alphanumeric or national") had no arm for a register at all, because the binder
carries a register as a computed operand, so `UNSTRING PAGE-COUNTER` and `UNSTRING LINAGE-COUNTER` compiled and split the
register's image; all three registers are now refused (COBOLNET1651).

**Sibling sweep.** The arms of the register dispatch: image (`OperandText`), integer test (`IntrinsicResultType`), relation
integer operand (`RelationComparability`), the algebraic fold and class conditions (`IntrinsicBinder`, `ConditionRenderer`),
LENGTH / BYTE-LENGTH (`IntrinsicBinder`, both twins), STRING / UNSTRING (`StringUnstringBinder`), INSPECT (already refused,
the PAGE-COUNTER receiving place carries the profile) and the runtime carriers; every arm now reads `DescribeCounter`.
`MOVE LINE-COUNTER TO` a `PIC X(4)` item stays `0000`. A 106-case sweep of every refusal case that names a register still
reports its recorded code at every refuse-at edition.

**Goldens.** Updated to the 10-digit, 4-byte description: `cases/8.4.3.14-LINAGE-COUNTER/pb1153_counter_register_character_image`
(its PAGE-COUNTER STRING and alphanumeric-relation legs became refusals), `pb1153_counter_register_byte_length`,
`cases/_unassigned/algebraic_counter_registers` (HIGHEST-ALGEBRAIC 4294967295), the PB1401 comment. New under
`wiseowl/cases/`: `8.4.3.15-Report-counters/pb1746_report_counter_description` and `pb1746_page_counter_advance_wraps`
(the engine's own increment), and the refusals `pb1746-page-counter-string-sender`, `pb1746-line-counter-string-sender`,
`pb1746-page-counter-alphanumeric-relation`, `pb1746-page-counter-unstring-sender` and
`8.4.3.14-LINAGE-COUNTER/pb1746-linage-counter-unstring-sender`, each at all four editions. docs/CONFORMANCE.md section 3
carries the survey, the GnuCOBOL inconsistencies not copied (its `HIGHEST-ALGEBRAIC` of the register prints `294967295`,
its `MOVE` image is 9 digits while `DISPLAY` prints 10) and the determination. No inventory row closed (the note claimed none).

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 000001-20261024T051822Z; Conformance 10,287 ·
Unit 35,059 · Characterization 5 cases ran in leg 1, the new goldens and `ReportCounterDescriptionDriftTests` among them).

Review round 1 (R1-1, blocking): the registers' unsigned 32-bit shape reuses `Usage.BinaryLong`, yet STRING SR1 and the §8.8.4.2.5 relation refuse them while `MOVE PAGE-COUNTER TO` an alphanumeric item compiles, where `MOVE` of a declared `BINARY-LONG UNSIGNED` item is COBOLNET0819 (§14.9.25.3 SR8). Answer: SR8 keys on "a data item described with usage binary-long", a USAGE clause a program wrote, and §8.4.3.15.4 GR1 gives the counters none, so only Table 16's numeric-integer row judges the move (legal, and cobc accepts it), whereas STRING SR1 and §8.8.4.2.5 read the implicit usage ("described implicitly or explicitly"). Rejecting the MOVE would reject legal source on a latitude the standard leaves open. The decision is now written where it is made (`MoveTable16.ShapeRefusal`, `PicInfo.ReportCounterItem`) and in docs/CONFORMANCE.md section 3; the golden `pb1746_report_counter_description` already pins the legal MOVE (PC-X4, LC-X14).

