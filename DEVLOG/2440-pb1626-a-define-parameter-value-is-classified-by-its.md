## Entry 2440 — 2026-10-23 22:40 PDT — PB1626: a `>>DEFINE … PARAMETER` value is classified by its spelling, not by System.Decimal

**Re-probe.** `PVAL=1234567890123456789012345678901` (31 digits) and `QVAL=1,000`, with `>>DEFINE PVAL AS PARAMETER` and
`>>IF PVAL > 0`: refused at 2002 and 2023 alike (COBOLNET1619, 'Alphanumeric' vs 'Numeric'), and `QVAL` was numeric. The cause was
`decimal.TryParse(env, NumberStyles.Number, …)` in `ConditionalCompilationProcessor.ApplyDefine`: System.Decimal holds 28-29
significant digits, so a legal 29-31-digit fixed-point literal (§8.3.3.3.2) became alphanumeric, while `1,000`, padded and
trailing-sign values, which are not a numeric literal's spelling, became numeric.

**Decision (the note's own survey; §7.3.11.4 GR4 leaves the form of the value to the implementor, so GnuCOBOL 3.2 decides, CLAUDE.md
rule 1).** The classification is syntactic and the same at every edition. It lives at ONE site,
`DefineParameterValue.Classify` (new): an empty text, a text beginning with a space, and a text that begins with a quotation
symbol without being exactly one closed literal are no value (the name is not defined, GR4 second sentence); a quoted literal is
that literal; the spelling of a fixed-point numeric literal is numeric of any length; any other text is alphanumeric as given. The
spelling test is `NumericLiteral.IsFixedPointLiteralForm` (§8.3.3.3.2 1)-3)), extracted from `IsFloatingPointForm`'s local helper
so there is one such test. `5.` is alphanumeric, where GnuCOBOL calls it numeric: §8.3.3.3.2 3) forbids a rightmost decimal point
(checked with `cite.py --check`). A numeric or quoted value is then evaluated by `EvaluateOperandText`, the path `>>DEFINE X AS
<literal>` takes, so its value is the `CtNumeric` carrier's and the digit capacity and literal screening are that path's.

The two questions the note left open: an over-long (more than 31 digits) spelling is refused at the DEFINITION, naming the
directive, exactly as `>>DEFINE X AS <32 digits>` is (GnuCOBOL defers to the use; only the position differs); an empty variable
leaves the name undefined. `>>DISPLAY PARAMETER` shares the variable lookup (`ParameterText`) and keeps transferring the text
unchanged, an empty variable as an empty image (DOC-A.1-55 now says the classification is the DEFINE directive's own).

**Sibling sweep.** No other `decimal.TryParse`/`NumberStyles` in `Cobol.Net.Frontend` classifies a directive value (the rest is
`CompileTimeArithmetic`'s documented System.Decimal mode); `GetEnvironmentVariable` has one directive reader; the other arm of
the dispatch, `>>DISPLAY PARAMETER`, was read and left (no GnuCOBOL basis exists for it).

**Goldens.** Unit tests, because the conformance harness cannot set the compiler process's environment:
`ConditionalCompilationDefineTests` (category theory with 14 values, no-value theory with 8, the 31-digit value at 2002, 2014 and
2023, the 32-digit refusal, the omitted branch) and `DefineParameterValueTests` (the classifier and the spelling test). The old
theory's `" -2.5 "` and `1,000` rows described the System.Decimal rule and were replaced. Rewrote `docs/CONFORMANCE.md`
DOC-A.1-49 and added the rule to DESIGN-compile-time-expressions.md §9. No inventory row claimed or closed (`closes_rows: []`,
reason in the note); `audit_annex_a1.py --check` reports no findings.

**Self-review.** Silent failure: an improperly quoted value makes the name undefined without a diagnostic, as GnuCOBOL does; the
first use of the name reports it as not previously defined. The semgrep counts are unchanged (`cobolnet-no-decimal` 2 → 2). No
diagnostic codes were used.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 8,214 · Unit 1,314 · Characterization 31 … Conformance 10,129/18,343 · Unit 34,605/35,919 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s)` (run 000001-20261024T045644Z). The Linux gate is the lander's.

