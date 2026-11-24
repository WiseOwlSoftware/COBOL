## PB6139: the exception-selection operands of USE and PERFORM Format 3 are written once and resolved once

**Mechanism (kb/Work PB6139, remainder 1 of PB2723, R2 finding lexer-grammar-1--duplication--f2#3).** §2023:14.9.28.4 GR17 makes
a Format-3 PERFORM WHEN match the USE statement's ("The rules for determining a match are specified in General rules 3a to 3g of
the USE statement"), so a WHEN operand is a USE operand. PB2723 unified the bound operand (`BoundExceptionSelector`) and its
rendering; the grammar still wrote the operand twice. Re-probed on ddf3400a61: `useEcEntry` and `performWhenEcItem` in
`CobolControlFlow.g4` were the same rule body (`cobolWord (FILE fileName)*`), bound by two loops (`EcBinder.ResolveWhenOperands`,
`ProcedureTableBuilder.DeclBindUseF3`) that each resolved the name, checked the EC-I-O pairing and resolved each file; the
mode alternatives INPUT | OUTPUT | I_O | EXTEND were written inside `useOnTarget` and `performWhenModeList`, and the new drift
test found a third copy, the OPEN statement's `openMode` (CobolIO.g4), read by its own mapping (`SequentialIoBinder.MapOpenMode`).

**The fix.**
- Grammar: one rule `exceptionSelectionOperand`, referenced by USE Format 3 (`exceptionSelectionOperand+`) and by
  `performWhenEcList` (its gated list); `useOnTarget` and `performWhenModeList` take the OPEN statement's `openMode` for their mode
  arm. The file-name lists stay per statement on purpose: a WHEN list's continuation is gated by `whenOperandAhead()` because a
  statement follows it, and gating USE's list would refuse a file named PARSE there.
- Binder: `ExceptionSelectionOperands.Resolve` is the one resolver of the operand (name funnel, EC-I-O pairing, each paired file),
  with each statement's own refusal (SR16 COBOL1601 / SR13), file resolution and recording passed in, called in source order so
  diagnostics keep their order. `OpenModeOperand.Of` reads the `openMode` keywords once (OPEN's `BoundOpenMode`) and
  `ExceptionModeIndex` gives the GR3b tier its `FileOpenMode` ordinal; `ExceptionOpenMode` and `MapOpenMode` are deleted. USE's
  Format 1 and Format 3 file resolution, which repeated the unknown-file and sort-merge refusals, is one `ResolveUseFile`.
- `ExceptionSelectionOperandDriftTests` holds that no rule but `exceptionSelectionOperand` writes the name/FILE operand, no rule
  but `openMode` offers the four modes as sibling alternatives, and that the five operand positions name the shared rules.
- `docs/CONDITIONS_EXCEPTIONS_DESIGN.md` names the shared rules and resolvers.

**Rows.** No row was GAP. Witnesses re-sited through record_verdicts batches: FMT-14.9.49.2 and 2002-FMT-14.8.45.1 retire
`#useEcEntry` and gain `#exceptionSelectionOperand`, `CobolIO.g4#openMode` and `ExceptionSelectionOperands.cs#Resolve`;
SR-14.9.49.3-12's notes name the new rule; 1985-FMT-VIII.4.4.2 retires `#MapOpenMode` for `OpenModeOperand.cs#Of`. GAP unchanged in
every edition (2023 1 → 1, 2002 1138 → 1138, 1985 0 → 0).

**Behavior-neutral.** No golden: the change adds no rule and changes no output. `python scripts/arch/compare_oracle.py` reported
`=== ARCH-ORACLE: IDENTICAL — 23569 cases (baseline ddf3400a61ec, new ddf3400a61ec-dirty) ===`.

**Self-review.** Looked for: a second copy of either operand left in the grammar (the drift test, which found `openMode`); a
diagnostic whose order or position changed (the resolver calls back as it resolves; the oracle's DIAGNOSTICS compare is
identical); a recovered parse reaching the new throwing arm of `OpenModeOperand.Of` (`OPEN F1.` reports COBOL5506 and never
binds); unused usings left by the move (one removed).
