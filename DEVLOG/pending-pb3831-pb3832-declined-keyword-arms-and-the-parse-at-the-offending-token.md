## PB3831, PB3832: a declined clause's printed keywords are all arms, and the parse hints read the offending token's position

Two fuzz findings the owner approved on 2026-10-26 ("Approve the CLASS IS BOOLEAN and COBOLNET2072 wording fixes too"),
under the standing rule of the same morning: "We always strive to fix the root causes, never simply making the
diagnostic pass". Both refusals were right and both messages were false.

**PB3831, `CLASS IS BOOLEAN`.** Re-probed: `r0901.cob` drew COBOLNET0901 ("'BOOLEAN' is a reserved word ... cannot be
used as a user-defined word") at 2002, 2014 and 2023, where every other operand of the declined CLASS clause draws
COBOLNET1708. `validateClassOperand` had no BOOLEAN arm, so the reserved word fell to `cobolWord`, which refuses it, and
the parse failed inside the declined construct. The file's comment called that "a plain syntax error inside a construct
this compiler declines"; it was a false statement about a word §13.18.11.2 prints as the clause's own keyword. FORMAT
in VALIDATE-STATUS (PB693) and the class condition's BOOLEAN (PB857) were the same mechanism. The fix adds the arm,
replaces the comment, and makes the next omission fail a test: `DeclinedFacilityDriftTests.EveryPrintedKeyword_IsAnArmOfItsRule`
reads each declined rule's cited general format from `specs/ISO_COBOL.md` and requires every printed word with its own
lexer token to be an arm the rule reaches, never through `cobolWord` (a tail rule also counts what its host
`valueClause` parses). A companion fact holds every keyword-writing rule of the file to a citation, and a can-fail
theory removes the BOOLEAN arm and the tail's VALID arm. Witnesses: a per-operand theory over all seven printed
operands (COBOLNET1708, and no COBOLNET0901 or COBOL0001) and the refusal `declined-validate-class-boolean`. The sibling
sweep over every grammar rule that mixes keyword arms with `cobolWord` (`acceptSource`, `validateStatusStage`,
`classConditionName`, `concatOperand`, `screenName`) found no other short list.

**PB3832, the false missing-statement claim.** Re-probed: `r14.cob` (an IF after an open `IF .. ELSE IF .. END-IF`, its
condition `LENGTH OF WS-OUT = 30`) drew COBOLNET2072 "the IF statement's general format shows an imperative-statement
here" plus COBOL0001, while the lone IF drew a bare COBOL0001; `IF A NOT <> B` drew COBOLNET2072 at NOT. A probe matrix
(IF, ELSE, EVALUATE WHEN and WHEN OTHER, inline PERFORM and PERFORM UNTIL, SEARCH WHEN, ON SIZE ERROR, NOT ON SIZE
ERROR, AT END, INVALID KEY; empty versus failing inside; at depth 0 and inside the open nest) showed the reach: every
nested failure drew the false claim, every nested empty block was named IF, the scope-terminator hint said "END-SEARCH
appears without a matching SEARCH statement" of a SEARCH that was open, and the wholly empty inline PERFORM drew no
named diagnostic at all. Two faults. A failed adaptive prediction reports the token where every alternative died while
the parser stays at the decision's first token; the inner IF's optional END-IF is a dangling decision whose
full-context prediction looks through the next statements, so the hints read the parser's state at the END-IF. And the
emptiness test (every statement start expected, the token none) was also true after a complete statement and after a
condition the token wrongly continued.

The fix gives every hint one view of the parse AT the offending token (`OffendingPosition`), built on the
full-context walk the closed-format residue hint already ran (renamed `ResimulationWalk`, since it now serves all of
them). A block is empty exactly when one CONTINUE stepped from a configuration that begins a statement directly in a
`statementBlock` lets the offending token follow, and the owner is named from that configuration's stack. The
figurative-spelling hint reads the expected set there, and the rule-stack hints (the scope terminator, MOVE, SPECIAL-NAMES,
INDEXED BY, FILE-CONTROL) read the rules open there. A second report at the token the previous report named is
suppressed: the recovery resumed at the decision and met the same fault, which is why r14 drew COBOL0001 twice. After
the fix r14 and the lone IF draw the same single COBOL0001, every empty block in the matrix draws COBOLNET2072 naming its
own statement at both depths except the depth-0 conditional phrases, and no failing block draws it. The golden
`pb1034-not-lteq` expected the false COBOLNET2072 and now expects COBOL0001. Pinned by four new tests in
`RequiredImperativeStatementDriftTests`; DESIGN-frontend-grammar §3.11 records the new test and closes its "known message
gap" for the empty inline PERFORM.

Branch review round 1 returned two blocking findings, both fixed at the root. R1-1: the walk gave up sixteen tokens
past the parser, so inside the open nest an empty block written after a long statement (`PERFORM UNTIL A = 1 AND … END-PERFORM`)
drew a bare COBOL0001, and the one-report rule then suppressed the recovery's named report. The dangling decision looks
to the end of the sentence and the walk now follows it there; the bound limits only where a closed-format residue may
stand, and the residue reading shares the report's one walk instead of building a second (lead R1-3). R1-2: at depth 0
every paired conditional phrase, empty or failing inside, drew COBOL0307 "A period may be missing" at the phrase word.
The phrase is an optional decision whose exit ends the statement, and SLL prediction, which does not know what follows a
rule it leaves, took the exit when the phrase's own alternative died later. So where the parser stands at the reported
token and the construct it completed just before it, re-simulated in full context from its own start, takes that token,
the report moves to the token where that re-simulation dies and is read there (`OffendingPosition.CarryForward`); a report covers
every token from the parser's position through the one it names. A probe matrix over twelve phrase forms (ADD, COMPUTE,
READ, WRITE, STRING, CALL; empty and failing; depth 0 and nested) now draws one diagnostic per row, the same at both
depths: COBOLNET2072 naming the statement, or the lone statement's COBOL0001 at OF. The predecessor matrix's two other
depth-dependent rows agree now too (EVALUATE's empty WHEN OTHER, and `IF X NOT <> 1`, which drew two reports nested). A
prediction refused by a semantic predicate at the token is not carried (the walk evaluates none; `OBJECT-COMPUTER. XYZ
FOO IS.` keeps its 0g refusal), and a structural hint (priority 0 or less) at the token where the parser stands outranks
the move: it diagnoses the token as written, the furthest point some parse reached does not. The one structural arm that
read token adjacency as a position fault, WHEN OTHER's (0b), now asks the same question: `WHEN OTHER IF LENGTH OF X`
drew COBOLNET2073 "a WHEN OTHER phrase may be specified at most once" at depth 0, where the phrase is in place and the
IF fails at OF. The whole Conformance population (18,844) and the whole Unit population ran green on the change but for
one pinned row, `GeneralFormatViolationTests` PBGF38 (`READ F1 AT END DISPLAY "E" AT END DISPLAY "F" END-READ`): its
second AT continues the DISPLAY's screen AT phrase, that parse dies at END, and the row now draws the COBOL0001 at END it
draws inside an open IF instead of COBOL0307 at AT. The rename was carried to the test class
(`ResimulationWalkNestingTests`, lead R1-4).

Self-review: the new walk methods allocate only on an error report, and the three new readers share one walk per report; the per-path owner search
is bounded at eight frames; `cite.py --check` passed on every clause cited; `semgrep/verify.py` and the citation audits
are unchanged.

Gate (run 000001-20261026T191128Z, head 70be735c0): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 10,858/18,844, Unit 37,429/38,430, Characterization 7/36 cases ran; Linux half (WSL clone of HEAD) GREEN).
