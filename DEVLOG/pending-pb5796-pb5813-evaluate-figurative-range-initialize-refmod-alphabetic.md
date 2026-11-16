## EVALUATE figurative range ends and INITIALIZE of an alphabetic slice (PB5796, PB5813)

Two code-read defects, one binder family: a screen or a category choice that treated an operand's class as unknown or fixed
where the standard gives it a definite answer.

**PB5796.** `EVALUATE N WHEN SPACE THRU 9` and `WHEN 1 THRU HIGH-VALUE` (N PIC 9(3)) compiled and printed `IN` at 85 and 2023,
while `"A" THRU 9` was refused COBOL2107. `ClassOf` answers null for a figurative (two candidates), and `ScreenRangeOperands`
tested same-class only when both ends were non-null. SPACE, QUOTE, HIGH-VALUE and LOW-VALUE are character values
(§2023:8.3.3.6.4 GR1; at 1985 nonnumeric literals, §1985:IV.4.2.2.2.1.3 GR3), never numeric; only ZERO takes its partner's
class (§1985:VI.6.13.4 GR1b). Fix: `IntrinsicArgumentRules.CanShareClass` asks that the two ends' `CandidateClasses`, projected
through `TableTwoClass`, intersect (an empty set still fails open), and `ScreenRangeOperands` calls it when an end abstained.
Both repros now exit 65 with COBOL2107 at 85 and 2023; the controls (`"A" THRU 9` refused, `ZERO THRU 9` legal) are unchanged.
Goldens: `pb5796_range_figurative_class_refused`, `pb5796_range_figurative_high_value_refused` (refused at 85/2002/2014/2023),
`pb5796_range_figurative_class_accepted` (ZERO THRU 9, SPACE THRU "Z", SPACE THRU HIGH-VALUE run at 85).

**PB5813.** `INITIALIZE WA (2:1) REPLACING ALPHABETIC DATA BY "Q"` over `PIC A(3)` left the slice alone and the ALPHANUMERIC
form filled it: the `RefModPlace` arm of `BindInitializeTarget` hard-coded alphanumeric, though §2023:8.4.3.3.4 GR6 and
§1985:IV.4.3.8.4.4 GR5 rewrite only edited and numeric categories. The arm now asks the new `RefModInitializeCategory`, which
reads `Table16Operand.Of(Place)` (the one slice-category reader) and keeps alphabetic, national and boolean. Probe before:
`M1[abc] M2[aQc]`; after: `M1[aQc] M2[abc]` at 85 and 2023. Golden `pb5813_initialize_refmod_alphabetic` (also pins the PIC X
slice: ALPHANUMERIC writes, ALPHABETIC does not).

Rows: `SR-14.9.13.3-4` and `GR-14.9.20.4-2`, `-4`, `-5`, `-6`, `SR-14.9.20.3-8` PARTIAL -> CONFORMS (GAP 38 -> 32); the three 1985
rows gained their witnesses.
