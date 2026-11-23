## PB3227: a CALL argument's pass mode is one rule, and the storage claim asks it

PB3227 was `half`. Its CSharp-spelling half had already landed as PB3229, so the open half was the R2 duplication finding
`data-binding-2--duplication--f1#5`: the data pass decided each CALL argument's pass mode itself
(`StorageClaimScan.ByReferenceOperands`), apart from `CallBinder.BindCall`.

**Re-probe.** The two copies disagreed, and the result was a wrong answer. The scan applied §2023:14.9.4.4 GR5, a Format 1
rule, to every CALL. The binder applies GR9 to a Format-2 keyword-less argument. In
`CALL "PB3227B" AS NESTED USING BY VALUE N NUM NUM` against `USING BY VALUE V BY REFERENCE A B`, with `AX REDEFINES A`,
the callee's `MOVE "5678" TO AX` left B at `1234`, although §2023:14.2.3 GR8 makes A, B and NUM one storage area. NUM was
never claimed onto a cell, so the area formal A held a private copy. The same miss happened after BY CONTENT, with a bare
program-prototype-name (Format 2 without the AS phrase), and with a keyword-less identifier that a later argument's B-AND
put into the parse's boolean node (the main checkout's build printed `B: 1234` for that case too).

**Fix.** The new class `CallArgumentModes` states GR5 and GR9 once. It absorbs `CallBinder.CarriedModeAfter` and the
local `Gr9NonIdentifierMode`. Every keyword-less arm of `CallBinder.BindCall` asks it, and so does the scan. A Format-2
callee's formals are bound after the claim, so the scan claims a Format-2 keyword-less identifier the way GR9 a) 1. passes
it to a BY REFERENCE formal. An unneeded claim costs a cell; a missing one breaks GR8. For a CALL without the AS phrase,
the scan records both formats' claims, and the asking element decides which applies by checking whether the bare target
word is a program prototype (§2023:8.4.6.8, `DeclaresPrototypeName`). User-function arguments were already decided at the
ask in the same way. Every operand the scan records now goes through GR8's one reduction,
`ConditionBinder.ReduceArgument`, and the scan's own text-matching copy is deleted.

**Tests.** A new `StorageClaimScanTests` case covers both formats, the bare prototype word, and the boolean-node CALL and
INVOKE arguments. The golden `14.9.4-CALL/pb3227_format2_keywordless_shares_storage` (accepted at 2002 and 2023) has four
legs, all `B n: 5678` / `AFTER n: 5678`. Its refusal at 85 is `pb3227_format2_keywordless_85`. No inventory row changes.

**Self-review.** Architecture: one rule, two askers, and no format decision left in the scan. Code: every keyword-less
CALL arm uses the shared rule; the INVOKE arm was already per-argument (§2023:14.9.23.4 GR6) and now uses GR8's reduction.
Performance: the scan still makes one walk and adds one record per operand. Duplication: `OperandIdentifier` is deleted.

**Review fix (R1-1).** A container claims its GLOBAL items' storage from its scan of its contained programs, and it judged
their bare CALL targets in its own prototype scope. That scope lacks a contained program's own name, which §2023:8.4.6.8
makes a program-prototype-name for that program's CALLs, so a RECURSIVE contained program's `CALL INNERP USING BY CONTENT N
NUM NUM` was claimed as Format 1 while its bind passed NUM BY REFERENCE (`B: 1234`). The scan now records whether the word
names a contained program holding the CALL. Golden `pb3227_contained_own_name_shares_storage`: `B: 5678` at 2002 and 2023.
