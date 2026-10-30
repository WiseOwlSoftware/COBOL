## Entry 2762 — 2026-10-29 19:47 PDT — PB2024: the grammar audit asks the inverse questions — an underlined word the grammar lets be omitted, a printed-once word it lets repeat — and its first full run finds SEND's END-SEND

### The gap

`scripts/spec/audit_grammar_optional_words.py` joined every measured general format to the grammar in ONE direction: a
word the page prints WITHOUT an underline that a rule demands (§8.3.2.4.3, the rejects-legal-source family). It never
asked the inverse, so the under-rejections of that join were each found by hand: PB755 (`lockOnPhrase : WITH? LOCK ON?
…` accepted `WITH LOCK MULTIPLE RECORDS`), PB756 (`ALTERNATE RECORD? KEY? …` accepted `ALTERNATE KEY IS k`) and PB1183
(`unstringIntoPhrase+` repeated the one printed INTO). Row SR-8.3.2.4.2-1 (required words) sat at PARTIAL for exactly
that reason.

### Why underlining alone cannot answer it

§5.2.2 makes an underlined keyword required "subject to the conventions specified in 5.2.6", and brackets (§5.2.6.2)
and choice indicators (§5.2.6.4) legitimately make an underlined word omissible: in `[ WITH LOCK ON [ MULTIPLE ]
{ RECORD / RECORDS } ]` MULTIPLE is omissible and ON is not, though both are underlined. So the audit needed the
printed format's OPTION STRUCTURE. The new `scripts/spec/figure_options.py` reads it from the PDF the same way the
transcription's figures are drawn: multi-line brackets, bars, braces and parentheses are paired by
`render_figure.enclosures` (which now returns a typed `Enclosure` naming its family and how low its opener reaches,
its three call sites and the self-test fixtures updated), and one-line delimiters, which the PDF sets as TEXT
(`[ MULTIPLE ]`), are paired on their own line. Each printed word gets its holders (outermost first), the diagram it
belongs to (a `Format N` label or a prose line ends one) and whether an ellipsis repeats it (§5.2.7: the bracket or
brace whose closer stands immediately left of the ellipsis, or the word itself). Inside one holder, words on different
lines are stacked alternatives unless a nested delimiter spans the other's line.

### The two new classes

- `required-word`: an alternative lets a word be omitted (`?`/`*`, an optional group or rule) while every printed
  occurrence of a word the alternative demands stands in sequence with an underlined occurrence of it that no bracket
  or choice indicator holds apart (nor a brace in which it is one of several stacked alternatives).
- `repeated-word`: an alternative lets a word repeat (`+`/`*`) while no printed occurrence lies in a repeated portion
  or among choice indicators (whose "one or more … in any order" a grammar can only spell as repetition).

A later phase's refusal is named in the rule by `// audit:required-word <WORDS> decided-by <Type>.<Member>` or
`audit:repeated-word`, the PB3751 marker generalized to three kinds and to several words per line, checked the same
way (the member must exist; a dead marker fails a full run).

The first full run reported 62 inverse candidates (49 of them repetitions inside choice indicators), and reading each against the rendered page showed most were the
join's own over-reach, each fixed structurally rather than listed: a compiler directive's format (§7.3, `>>IF`,
`>>EVALUATE`) was being joined to the statement rule sharing its keyword (now the preprocessor's, unjoined); a rule
merely MENTIONING the keyword optionally (`valueClauseFalsePhrase`'s `WHEN SET TO FALSE`) was anchored to the SET
statement (a rule DEMANDING the keyword now anchors first, `ANCHORINGS = demanding, named, enclosing`); `demanded`'s
sibling subsumption read `FUNCTION functionName+ INTRINSIC` as making INTRINSIC omissible beside another specifier
(only a marked-optional element omits now); a word an arm also writes through a required rule (EVALUATE's WHEN, the
second LOCK) was read as omitted; PERFORM's until-phrase EXIT was joined to the PERFORM format above it. The forward
class's six open candidates are the same before and after.

### What it found

A control run that re-introduced PB755's `ON?`, PB756's `RECORD?` and PB1183's `unstringIntoPhrase+` reported all three.
On the tree: 0 undecided candidates in either new class, 16 DECIDED. Each decided one was probed: SUBTRACT without FROM
is COBOL1689 (`ArithmeticBinder.BindSubtract`), `CONTROLS ARE CX FINAL` and `FINAL FINAL` are COBOL2421 and `PAGE … HEADING
1 HEADING 2` is COBOL2423 (`DataBinder.BindReportDescriptionClauses`), OCCURS DYNAMIC's repeated or reordered phrases are
COBOL2789 (`DataBinder.NarrowToDataDivisionFormats`). One was a real under-rejection: §14.9.38.2 prints `END-SEND` on a
line of its own WITHOUT brackets in both SEND formats (PDF page 756, rendered), and `SEND TO 'SRV1' FROM A RETURNING B.`
compiled with only the declined-facility warning. Read on the same page, Format 1's `RETURNING data-name-1` line is
unbracketed too, and a literal-1 operand is printed in Format 1 alone. `DeclinedFacilityBinder.ScreenSendRequiredPhrases`
now refuses both with COBOL1850, the one unbracketed-phrase screen (`StatementValidation.ScreenOmittedRequiredPhrase`):
the facility is declined, its syntax rules are not. The RETURNING of a SEND whose TO operand is a name stays
unscreened, since message-server-name-1 and data-name-2 are told apart only by resolving the name, which `BindSend`
deliberately does not.

### Goldens and the row

`cases/8.3.2.4.2-Required-words/pb2024-required-word-omitted` (MOVE without TO, refused at all four editions),
`wiseowl/cases/8.3.2.4.2-Required-words/pb2024-send-end-send-omitted` and `pb2024-send-literal-returning-omitted` (2023,
COBOL1850). `cases/_unassigned/optional_words_mcs` wrote `SEND "LITERAL-TAG" FROM MSG-BODY END-SEND`, a Format 1 SEND
without its RETURNING; it writes one now and its output is unchanged. SR-8.3.2.4.2-1 PARTIAL → CONFORMS (GAP 21 → 20).

### Branch review, round 1

The reviewer found one blocking regression (R1-1): the version-matrix probe `async-messaging-2023` in
`tests/version-matrix/constructs.json` wrote `SEND TO WS-TAG FROM WS-MSG.` with no END-SEND, so the new screen refused
it at 2023 (COBOL1850) where the row expects a clean compile. The gate had planned that cell into leg 2, so CI's merge
group would have been its first run. The probe now writes `SEND TO WS-TAG FROM WS-MSG END-SEND.`, and the row's
description says why. A sweep of every SEND statement in the tracked COBOL sources, test strings and matrix rows found
no other one without its END-SEND. The `async-messaging-2023` cells pass at all four editions.

### Stated limits

The audit measures the licensed PDF, so neither CI's `audits` job (no `specs-private`, by design) nor a worktree gate can
run its full join; both its self-tests and `figure_options.py`'s run in every gate through `self_tests.py`, on figures
measured off the page. `sweep_figures.py --check` reports one figure differing from the generator, §14.9.9.2's
`CONTINUE`, which is the transcription's deliberate C2 correction (kb/Work PB3751), not this change.

