## PB2144 — the TO / FROM / BY / INTO operand of the four arithmetic verbs is one grammar rule, so every operand-shape error is COBOL1689

**Re-probe (my build, base a9f2c2897).** The note said `ADD A TO 2 3 GIVING C` drew a raw COBOL0001. On today's tree it
draws COBOL2269 ("unexpected '3'. The ADD statement ends before this token"), and the split was wider than the note
said. A second operand or a ROUNDED written in a GIVING form, or a literal written after a receiver in Format 1, was a
parse error at ADD TO, SUBTRACT FROM, DIVIDE INTO and DIVIDE BY. MULTIPLY BY named the same mistake with COBOL1689,
and even MULTIPLY parse-errored on ROUNDED after a function-identifier. The sibling sweep found a worse arm in the
same file. BindDivide's REMAINDER branch (Formats 4 and 5) bound the INTO phrase whole and never asked the Format-2
check, so `DIVIDE A INTO B C GIVING D REMAINDER E` and `DIVIDE A INTO B ROUNDED GIVING D REMAINDER E` compiled clean,
with the second operand and the ROUNDED silently dropped.

**Fix shape.** ISO §14.9.2.2, §14.9.44.2, §14.9.26.2 and §14.9.12.2 print this position twice. Format 1 writes
receivers, `{identifier-2 [rounded-phrase]} …`. Every GIVING format, DIVIDE Formats 2–5 included, writes ONE sending
`{identifier-2 | literal-2}` with no ROUNDED. The grammar now writes the union once,
`arithmeticPhraseOperand : (receivingIdentifier | literal | functionCall | inlineMethodInvocation) roundedPhrase?`, and
puts it with `+` after TO, FROM, MULTIPLY's BY, INTO and DIVIDE's BY. `receiversOrSendingOperand`, `multiplyByOperand`
and `receivingOperand` are deleted. A shape some format does not print therefore always reaches the binder, and
ArithmeticBinder judges it with two checks. `GivingSendingOperand` (renamed from Format2SendingOperand, since it also
serves DIVIDE Formats 3–5) reports a second operand with its count and spellings, and reports each ROUNDED. Each
message names the verb, the phrase and the format. `Format1Receivers` is unchanged.
`ArithmeticOperandRole` now classifies one mixed rule, and a receiver is a `receivingIdentifier` child.
ExpressionBinder's three arithmetic `Receivers` overloads now share one `ResolvedReceiver`, and the MULTIPLY-only
overload is gone. The REMAINDER arm's two conditional expressions became two plain arms. The owner exempted this one
grammar change from the token-stream differential (DESIGN-architecture-review §8.4), so its oracle is the whole gate
plus the goldens. DESIGN-frontend-grammar §3.15 now explains why the rule is written as wide as its formats.

**Goldens.** There are eleven refusals, one per file, under `cases/14.9.{2,12,26,44}-*/pb2144-*`, each expecting
COBOL1689 at 85, 2002, 2014 and 2023 (the MULTIPLY function-identifier one at 2002 and later). Before the fix, nine
were COBOL2269 and two compiled clean. `cases/14.9.12-DIVIDE/pb2144_divide_phrase_sending_operands` (accept 85) pins
Formats 2–5 with literal and identifier INTO and BY operands. Its values come from §14.9.12.4 GR1, GR6c and GR7 and
§14.7.4.1 truncation, and it is the corpus's first program that writes Format 4 at all.

**Rows.** None claimed and none closed. A `record_verdicts` batch retired the two deleted grammar rules and the
renamed method from FMT-14.9.26.2 and FMT-14.9.44.2 and restated their prose. It added the new witnesses to
FMT-14.9.2.2, FMT-14.9.12.2 and FMT-14.9.44.2, and restated the stale code quotations in GR-14.9.12.4-2 and
SR-8.4.3.4.3-1. GR-14.9.12.4-2's Format-4 residue is closed by the new golden. The GAP count stays at 1.

**Self-review and gate.** The CITATIONS audit turned the first gate RED (run 000001) on one line of the
ArithmeticOperandRole doc comment that named §8.4.3.1.2 Format 1 and Format 4 together; each now sits on its own line.
The four lenses found nothing else: the dropped-receiver path in `Receivers(ArithmeticPhraseOperandContext)` runs only
after `Format1Receivers` has passed, a GIVING-form statement with a violation is refused at bind, error-recovery
shapes (`ADD A TO GIVING C`, `DIVIDE A BY`) stay parse errors with no binder crash, and semgrep's counts are
unchanged. Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 21,424/21,424 · Unit 39,640/39,640 · Characterization
37/37 cases ran (skipped 0) (run 000002-20261104T000458Z)`, with all 44 pb2144 cases Passed in its Conformance.trx.
