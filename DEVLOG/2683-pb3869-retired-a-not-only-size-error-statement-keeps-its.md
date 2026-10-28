## Entry 2683 — 2026-10-27 17:48 PDT — PB3869 retired: a NOT-only SIZE ERROR statement keeps its receivers, and leg 1 keeps a file's changed method bodies

**PB3869 is retired, not landed.** The fuzz finding (fuzz-20261026-225310, fz00046) read §14.7.5's "If a NOT SIZE ERROR
phrase is specified, it is ignored" as making a statement written with only NOT ON SIZE ERROR the phraseless statement in
every respect, so that `ADD 1 TO X NOT ON SIZE ERROR ...` (X `PIC 9 VALUE 9`) would store the low-order 0 and
`DIVIDE 1 BY 0 GIVING Q NOT ON SIZE ERROR ...` would end the run unit. The owner approved the fix on that reading, and the
branch implemented it: `EmitArith` stopped emitting the checked shape for a NOT-only phrase, and a new
`ArithmeticEmitter.DetectingStore` ran each receiver checked, then unchecked from the same state when the checked form
raised, so the receiver took the phraseless landing while a flag still decided the NOT imperative. Two review findings
followed and were fixed (the right-hand side evaluated twice on a size error, and the REMAINDER store bypassing the
detection). Its gate was GREEN and the review CLEAN.

**The merge queue refuted it** (PR #716, merge-group run 40612629630): twelve CCVS85 programs (NC106A, NC170A-NC173A,
NC175A-NC177A, NC202A, NC203A, NC251A, NC253A) print FAIL* where their goldens print PASS, every failing test a NOT-only
statement whose receivers the CCVS expects UNCHANGED (`DIVIDE .01 INTO 15.44` into `PIC 99V99` keeps 15.44; the fix
stored 44.00). Reproduced locally at the merged head; main, carrying #715, is green. The adjudication: the "ignored"
sentence settles the NOT imperative only, and the no-phrase rules end in §14.6.13.1.3 item 8, where with checking off
"the implementor defines whether or not execution will continue, how it will continue, and how any receiving operands
are affected". ISO does not control the receivers, so CLAUDE.md rule 1 goes to GnuCOBOL, and GnuCOBOL 3.2 keeps an
overflowing resultant unchanged for any `[NOT] SIZE ERROR` phrase (`cobc/typeck.c`, `COB_STORE_KEEP_ON_OVERFLOW`), runs
on and skips the NOT phrase: main's behaviour all along. The one exception is a COMP-5, COMP-X or COMP-N receiver of a NOT-only statement, which GnuCOBOL stores as if no phrase were written (its `build_store_option` sets the flag for those usages only with ON SIZE ERROR);
the owner ruled that exemption GnuCOBOL's error, so WiseOwl keeps those receivers too (kb/Work PB3989).

**What lands instead.** The emitter, `EmitterState`, `BoundTree` and the two drift tests the fix edited are main's again.
CONFORMANCE.md DOC-A.1-70 states the NOT-only disposition (receivers unchanged, execution continues, a zero divisor too,
the NOT phrase skipped; GnuCOBOL and the CCVS cited), CONDITIONS_EXCEPTIONS_DESIGN.md says the checked shape serves it on
purpose, and the witnesses pin it: golden `14.7.5-.../pb3869_not_size_error_alone` (2002, 2014, 2023, every row computed
from the determination: ADD, MULTIPLY, COMPUTE ROUNDED, several receivers, an edited receiver, CORRESPONDING, a run-time
subscript, ON and NOT together, a REMAINDER past its receiver, one evaluation of FUNCTION RANDOM) and
`SizeErrorDispositionTests` (`NotSizeErrorAlone_CheckingOff_LeavesTheResultant_AndRunsOnlyWithoutASizeError` over both
lanes and fixed, edited and float receivers; `ZeroDivisor_NotSizeErrorAlone_LeavesTheResultantsAndContinues` over every
divide shape; `NotSizeErrorAlone_CheckingOn_RunsOnlyWithoutASizeError`).

**Why the local gate missed it (the CI invariant).** Leg 1 held none of the twelve: `impacted_tests.reach_cs` returned
at the first hunk outside a method (GuardedStore's signature change) and took ArithmeticEmitter.cs at the file level only,
so the tests that executed the changed method bodies fell from tier 1 to tier 2, which leg 1 never runs. Conformance tier
1 was 75 cases where it should have been 1,850. `reach_cs` now reads every hunk and adds the file level beside the
direct hits; replayed on the fix, leg 1 holds NC203A, one of the twelve, so the gate would have gone RED locally.
Self-test arm "a declaration hunk beside a method-body hunk"; DESIGN-test-build-ci.md §3.13 states the rule.

Split on the owner's order (2026-10-27 ~21:45 PDT), so the retirement lands without waiting on adjacent work: PB3989
(the owner's decision: GnuCOBOL's COMP-5 / COMP-X / COMP-N exemption is GnuCOBOL's error), PB3990 (an inexact ROUNDED MODE IS PROHIBITED value
overwrites a binary receiver on main, where §14.7.4.3 rule 7 keeps it), PB3991 (a version bump hides an emitter
change's NIST reach from leg 1) and PB3992 (a correct core change waits on every adjacent finding) are filed.
