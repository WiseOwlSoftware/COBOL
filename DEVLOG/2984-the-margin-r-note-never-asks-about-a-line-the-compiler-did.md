## Entry 2984 — 2026-11-01 22:44 PST — The margin-R note never asks about a line the compiler did not read, and a blank line keeps its own origin (PB4412)

PB4372 taught the between-lines arm of `CobolErrorListener.NoteMarginRCut` to skip a tokenless line that holds program text, on the
reasoning that a later stage omitted it. The residual was a line whose ONLY text is past margin R: its program-text area is blank, so the
`HoldsProgramText` test cannot tell it from a vanished statement, and it sits in a false `>>IF` branch or is a `D` line compiled without
debugging mode. The note then named a line the compiler never read.

**Re-probe.** On the Debug build, `CALL "SUBPROG" USING WS-MSG BY`, then a line with `DISPLAY WS-MSG` only from position 73 inside a
false `>>IF TRACING IS DEFINED` (and, separately, as a `D` line without `WITH DEBUGGING MODE`), then `STOP RUN.`: COBOL5788 "this line has
no program text" beside the COBOL5506 at STOP, both times.

**Fix shape: record the omission where the dropping stage knows it.** The filed shape (the CC stage records omitted origins) covers one
of the two kinds. (1) A TEXT stage's omission is now part of the line's origin: `LineOrigin.Omitted`, set with `AsOmitted()` where the
conditional-compilation driver drops an ordinary line (ISO 7.3.16.4 GR 3, 7.3.13.4 GR 4 and 5), carried by every later stage with the origin
(`LineOrigin.From`, `OriginWriter`), answered by `SourceLineMap.IsOmitted`, and refused by `FixedFormAreas.MarginRCutAt` (null), so the
next text stage that drops a line is one call and no reader keeps a list. (2) A debugging line is not a text-stage fact: whether a `D` line
is source is its unit's `WITH DEBUGGING MODE` (12.3.5.4 GR 1), which `DebuggingLineRewriter` reads off the tokens after the line map
exists. `Hide` now moves the MARKER to the ABSENT_DEBUG_LINE channel as well (a line with no text token was invisible in the stream),
`AbsentLines` names the hidden lines between two tokens, and the listener asks both. The two stay two mechanisms because the facts are
decided at two different times; each is recorded where its stage knows it.

**A blank debugging line was never seen by the rewriter.** The logical conversion DISCARDED a `D` line whose program text is blank (6.5
2), so no marker reached the lexer and the rewriter had nothing to decide. Its slot now holds the bare carrier
(`LogicalLineBuilder.EmitDebugging`); it still touches neither the join target nor the literal state, so a blank `D` line between the
parts of a literal reads as before (`ABlankDebuggingLine_BetweenTheLinesOfALiteral_StillContinuesIt`), and with the mode the line is
source and the note stays. The first gate caught three tests that pinned the old slot (`""`) and the marker's HIDDEN channel
(`LogicalLineBuilderTests` x2, `DebuggingLineRewriterTests`); they now pin the bare carrier and the ABSENT channel.

**Found in the same files (I9): a lone blank line lost its origin.** The control of the new test (the same past-margin-R line in the
branch the directive TAKES) drew no note, before the fix too. A block of one empty line between two directives went through
`ExpandCopiesOneLevel` and `OriginWriter` with no content to name its piece by, and was named `("", 0)`, a line of no file; a final empty
piece was named like the piece BEFORE it. `ExpandCopiesOneLevel` hands a one-empty-line block back as it stands and the writer names an
empty trailing piece by the empty input piece it was written for. So a past-margin-R-only line that is alone between two directives in a
taken branch is named again (it had drawn no note).

**Goldens and tests.** `MarginRCutNoteTests` +11 (omitted IF, omitted ELSE, unmatched WHEN, omitted branch of library text, each with its
taken control; debugging line without the mode, with it, in a unit without it; the live line beside an omitted one and beside a comment
debugging line; the blank-debugging-line literal), `SourceLineMapTests` +2. Conformance: accepted `pb4412-past-margin-r-in-omitted-branch`
(2002 on) and `pb4412-past-margin-r-in-debugging-line` (85, 2002); refused `pb4412-vanished-statement-beside-omitted-branch` and
`pb4412-vanished-statement-beside-debugging-line`. No inventory row changes verdict (`closes_rows: []`).

**Gate.** `=== BUILD-LOCAL GATE: GREEN` at db31e465d, run 000002-20261102T060344Z (Conformance 20,579/20,579, Unit 39,170/39,170, Characterization 37/37, no Linux half in the line); the first gate (000001-20261102T055830Z) was RED on the three tests that pinned the old blank-D slot, re-pinned above.

**Review round 1.** R1-1 (blocking): the blank debugging line now kept the bare carrier in its slot, and the conditional-compilation driver's text-before-the-first->>WHEN check read it as program text, so a legal program with a blank `D` line between `>>EVALUATE` and its first `>>WHEN` was refused with COBOL2649 (§6.5 2): a blank line is discarded; §7.3.13.2). Fixed at the root: `ReferenceFormatProcessor.HoldsNoProgramText` is the one answer to "does this logical line hold text" (the carrier alone, or spaces, is none) and the check asks it. Tests: `MarginRCutNoteTests` +2 (blank `D` lines pass; a `D` line with text is still text), conformance `pb4412-blank-debugging-line-before-first-when` (2002). R1-2: the omitted-WHEN citation is now §7.3.13.4 GR 4 and 5 everywhere (GR 5 is the no-true-WHEN rule the test builds; Format 2 repeats it as GR 9). R1-3: the two statements in `FixedFormAreas.MarginRCutAt` are on two lines.

