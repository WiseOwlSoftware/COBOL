## Entry 3716 — 2026-11-15 19:41 PST — MoveBinder: an edited sender is no longer edited twice (PB5678), and REWRITE FROM a user-defined function is legal at 2002 (PB5645)

Two mechanisms, both in the MOVE binder's reach.

**PB5678.** `MOVE SXE (PIC XBX, "A B") TO RX RX2` stored "A" in both receivers and PIC X/X "A/B" stored "A//", at every edition,
with no diagnostic. Re-probe on this build (Debug, `--std 2002` and `--std 2023`) reproduced the note's four lines exactly.
Cause: with two or more receivers `BindMoveOf` freezes the sender through `SendingValueTemp.Materialize`, and the temporary
was cloned with the sender's edit mask, so the identity MOVE into it applied the insertion editing to characters that
already held it (§2023:14.9.25.4 GR6 edits on every valid elementary move). Fix at the one description of the temporary,
`SendingValueTemp.OfPlace`: an alphanumeric-edited or national-edited sender is held in the unedited item of its category,
usage and length (GR1 leaves the intermediate to the implementor). Every `Materialize` caller (MOVE, CORRESPONDING element
moves, EVALUATE subjects, function arguments, FROM phrases) shares the derivation, so none needs its own arm. Sweep: numeric-edited
senders to several numeric, numeric-edited and alphanumeric receivers, a group holding an edited member, and an EVALUATE on an
edited subject all measured right on the fixed build. After the fix the repro prints `[A B  ][A B  ]`, `[A/B  ][A/B  ]`, `[C D  ][C D  ]`.
Goldens: `14.9.25-MOVE/pb5678_move_edited_sender_multi_receiver` (85, 2002, 2014, 2023) and
`14.8.24@2002-MOVE-STATEMENT/g2mov20_national_edited_sender_multi_receiver` (2002). `GR-14.9.25.4-1` DIVERGES to CONFORMS
(the 1985 row follows its chain) and `2002-GR-14.8.24.3-1` DIVERGES to CONFORMS.

**PB5645.** `REWRITE R FROM FUNCTION <user-defined numeric function>` was refused COBOL1914 at `--std 2002` by the
2023 any-function rule. §2002:14.8.33.2 SR9 (and SR8 with the FILE phrase) restrict only "an intrinsic function"; the
2002 WRITE and RELEASE rules are unconditional, so only the two REWRITE rows are affected. COBOL-2014 restates both as "a
function" (§2014:14.9.33.2 SR7 and SR8). `FromPhraseRules` gains the column `IntrinsicOnlyBefore2014` (true on the REWRITE and
REWRITE FILE rows), and `MoveBinder.ScreenFromFunctionClass` exempts a user-defined function (a signature
`IntrinsicBinder.SignatureOf` holds) below 2014, leaving the MOVE-validity rule to judge it. The REWRITE message now also cites the
2002 number. Re-probe: accepted at 2002 with `R1=[042]`; still refused at 2014 and 2023. Goldens: accepted
`14.8.33@2002-REWRITE-STATEMENT/g2rw09_from_user_defined_numeric_function`, refused `14.9.35-REWRITE/pb5645_rewrite_from_user_function_refused`
(2014, 2023) and `pb5645_rewrite_from_intrinsic_numeric_refused` (2002 to 2023, the intrinsic half of the rule stays enforced).
`2002-SR-14.8.33.2-9` recorded CONFORMS; `2002-GR-14.8.33.3-6` gains its witness.

Inventory: 2023 GAP 9 to 8, 2002 GAP 1805 to 1803.

