## PB906, PB3844: a statement that ends before a phrase its format does not print draws COBOL2269 naming it, never "A period may be missing"

Wave 20261711 group C, under the owner's standing campaign rules ("Fix the root causes, never simply making the
diagnostic pass"; "Always be consistent with ourselves barring an explicit requirement differently").

**What the re-probe showed.** PB906 (filed 2026-09-19) said hint 14 won on a COMPLETE statement carrying a phrase the
general format does not print. The branch it continued (PB3831/PB3832, merged first, fast-forward) had already moved the
measurement: `SUM WS-K + 1` parses (PB883), and the three goldens the note names drew a bare COBOL0001 or the true
COBOL0306. What still drew the wrong family was `WRITE SQ-REC IGNORING LOCK.` (golden `pb331-write-ignoring-lock`,
which PB3844 filed from the previous review): COBOL0307. The note's explanation was off by one reading. The walk is not
blind to a predicate: IGNORING is a context-sensitive word, so `. IGNORING` really parses as a paragraph header, and the
guess asked about ONE token. The LOCK after it is what no paragraph header takes. The fifth fuzz night's witness
(fz02341, `WRITE R INVALID KEY SET X TO "INVALID"`) already lands on the literal; what was left was its wording.

**The fix, one mechanism in three places** (`CobolErrorStrategy`).
1. `OffendingPosition.APeriodLetsTheSentenceFollow` (was `APeriodLetsTheTokenFollow`): the missing-period guess walks the
   period, the token and the rest of the sentence through its separator period (sixteen tokens at most), so a word that
   could head a paragraph but is followed by something no header takes is not a missing period.
2. Hint 0h: the fault is a statement written with a phrase its formats do not print there (an unprinted phrase, a bracket's
   second alternative, a repeat, a phrase out of sequence). `OffendingPosition.StatementThatEndedBefore` finds the statement
   the parser completed just before the token, re-simulates it over every path (`ResimulationWalk.Over`) to show it cannot
   take the token, and names the innermost statement whose format prints the word AFTER a statement it holds (`TokensAfterANestedStatement`, read from the ATN
   with the nested statements left out): a second ELSE names the IF, not the DISPLAY before it, and a SET format is named
   SET. The hint is COBOL2269 (statement-format-shape), cites ISO §5.2.1, and fires only where no structural hint stands and
   the token is no scope terminator (COBOL0306 names that). It is a structural answer: no new code was needed
   (COBOL5209-COBOL5211 returned unused).
3. Hint 15 ("a data-name is expected here, not a literal") stands only where no literal is expected: `SET X TO "A"` also
   expects an arithmetic-expression (§14.9.39.2 Format 1), and the old sentence told the user a literal was not allowed.
   `DiagnosticHint.IsStructural` and `HasStructuralHint` state the priority-0 convention once for the guards that each
   spelled it.

**The sibling sweep.** Every refused conformance case (2,884 in `diagnostics.json`) was re-run before and after on the
branch's own build: 14 changed. Thirteen are a stray phrase after a complete statement (COBOL0001 or COBOL0307 to
COBOL2269: the four pb1125 INSPECT repeats, pb1183, the READ/WRITE/REWRITE lock cases, pb712, pb758's END-INVOKE, l1c17's
extra ')'); one is a cascade (l1c27's second report, COBOL0307 to COBOL0001, a missing-period guess that was only true by
accident). The COBOL0307 that still wins (`l1c10`, `l1c20`, `pb1966`, `pb3390`, and the section, paragraph, END PROGRAM and END
DECLARATIVES variants probed by hand) is a true missing period each time. COBOL0312 and COBOL0305 are vague guesses and false
in none of the cases. Registrar 14f's stale hints (OCCURS KEY, SEARCH, EVALUATE ALSO) were already corrected or deleted by
PB1266 and PB2770. A first try that dropped every guess beside a structural hint centrally broke
`pb1266-occurs-key-after-indexed-by`, where hint 8 COMPLETES the closed-format refusal of 0g; the guards stay per hint.

**Witnesses.** `ParseRecoveryHintTruthTests` PBHT41-46 (MOVE, CONTINUE with IGNORING, ADD, the IF of a second ELSE, SET, a
MOVE nested in an IF) and PBHT38 (a context-sensitive paragraph-name after a CONTINUE is still a missing period); 12 goldens
re-pinned in `diagnostics.json` and the comments of the four pb1125 and two IGNORING cases; `GeneralFormatViolationTests`
PBGF03/05/18/21/24/27/36 and the three stray-')' pins of `DiagnosticPositionTests` moved to COBOL2269.
`pb331-read-two-from-one-lock-bracket` keeps COBOL0001 (at 2002 and later RETRY reaches bind as a reserved word, COBOL0901,
and the second report is the shared one).

**Self-review.** Architecture: one definition of "structural"; the statement's own tokens come from the ATN like every
other set in this file. Full code: `Over` is the re-simulation `CarryForward` already uses, so a construct the SLL
prediction closed early (`ADD ... ON SIZE ERROR END-ADD`) is never named. Performance: error path only; the own-syntax
sets are computed once per statement rule. Comments: the hint-14 doc and the DESIGN paragraph say the sentence, not the
token. Left as it was: the false COBOL0306 "END-x without a matching x" that follows a true diagnosis (PB3843).

**Review round 1 (R1-1).** The set that chose the named statement was every token the enclosing statement's syntax can
match ANYWHERE, conditions included, so `IF c MOVE 1 TO A > 2 END-IF` named the IF ("The IF statement ends before this
token") for a `>` that belongs to the IF's condition, which the MOVE follows: the IF had not ended and the MOVE had, and the
same stray `>` after a top-level MOVE named MOVE. The set is now the tokens an enclosing statement's format prints AFTER a
statement it holds (`TokensAfterANestedStatement`: the follow of each nested-statement call in the statement's own rules,
continued through the end of a rule into its caller up to the statement's end, from `ATN.NextTokens`): ELSE and END-IF for
IF, END-PERFORM, WHEN and the conditional phrases for the others. A second ELSE still names the IF (PBHT44); the `>`, `=` and
`AND` of a condition after a complete MOVE name the MOVE inside IF, PERFORM UNTIL and EVALUATE alike, as at top level
(PBHT47-51). R1-2 (a nonnumeric literal where only a numeric one is admitted now draws the bare COBOL0001) stays a lead: a
kind-naming hint needs a new code, and the codes returned unused are the pool's again.

**Gate.** Whole Conformance 18,956/18,956 and whole Unit green but the two absent-corpus `ExternalCorpusPopulationDriftTests`
(environment), before the gate. `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's
whole-population run: Conformance 11,669 · Unit 996 ... — Conformance 7,287/18,956 · Unit 37,460/38,456 · Characterization
36/36 cases ran (skipped 0) in 1 of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN · slot 1 of 4 ... run
000001-20261027T001256Z ===` at 453afb1ba.
