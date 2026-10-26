## PB3830: one grammar rule and one §14.5.3.3 decision for every paired conditional phrase

The fifth fuzz night's adjudicator called COBOL2796 on `SUBTRACT A FROM C ON SIZE ERROR ADD 1 TO D NOT ON SIZE
ERROR MOVE "N" TO F END-SUBTRACT` a refusal of legal source. It is not. The rule that decides which statement a phrase
belongs to is the paragraph that closes §14.5.3.3 (rendered, PDF page 566: it follows list item 4 unindented, so it is
§14.5.3.3, not the "4)" cite.py attributes it to): "Any phrase encountered is the next phrase of the most-recently
preceding unterminated statement with which that phrase may be syntactically associated." ADD prints [NOT] ON SIZE
ERROR, so the NOT phrase is the ADD's, the ADD is a conditional statement (§14.5.1), and SUBTRACT's
imperative-statement-1 may not hold one. The archived 2002 text carries the paragraph word for word. The slice had read
§14.5.1's "ended … by any phrase associated with that general format" as ownership; that sentence says where an
imperative-statement ends, and the format test is the statement's own, not its position's.

The owner then asked for the consistency to be grammatical: "all COBOL paired conditional phrases should be handled
grammatically in the same consistent manner". Eighteen per-statement rules spelled the phrases, in five shapes. They
are now ONE rule: `conditionalPhrases : ({conditionalPhraseAssociates($ctx)}? conditionalPhrase)*`, every phrase
keyword in `conditionalPhraseHead`, and the predicate is `ConditionalPhraseAssociation.Associates`: a phrase joins the
statement whose loop asks when one of the statement's printed formats, at the target edition, admits it beside the
phrases already written and it is not already written. ANTLR's greedy loop asks the innermost open statement first,
which is "most-recently preceding". The predicate reads `$ctx`, which makes it context-dependent: ANTLR then asks it
only in the rule's own decisions and treats it as true when an inner statement's prediction reaches an outer loop
through the follow links, where a context-free predicate would be evaluated with the inner context and kill the exit
path. That is also why the owners write the rule bare. Every binder now asks `ConditionalPhrases` for its family and
binds the pair by role through `PhraseBlocks.Bind`; the old rules and `PhraseBlocks.Split` are deleted.

The one table fixed three siblings the per-statement rules had wrong. At 2023, `STRING … NOT ON OVERFLOW CALL "P" ON
OVERFLOW …` was refused, though CALL has no ON OVERFLOW format at 2023 and the phrase is the STRING's. It now compiles
at 2023 and is COBOL2796 at 85–2014, where CALL's 1985–2014 format prints it. A superset reading keeps
`CALL … ON OVERFLOW` alone associated at 2023, so COBOL0882 still names the removal, but only when no enclosing
statement may take the phrase, so the superset never moves a legal program's phrase. Reading the edition there is
recorded as the one phrase-association read of `EditionReadDriftTests`. `CALL … NOT ON OVERFLOW` (no edition prints
it), `CALL … ON OVERFLOW … NOT ON EXCEPTION` and `READ … AT END … INVALID KEY` (two formats mixed) used to compile;
each is now refused COBOL2269 from the parser state, as is a second occurrence of a phrase. COBOL2796's message
quotes §14.5.3.3 when the inner statement took a phrase the outer statement's format also prints.

Re-probe: r2796 and the MULTIPLY/COMPUTE sibling are refused as before. The CALL/DISPLAY probe is legal and accepted
(device DISPLAY prints no EXCEPTION phrase). The same-verb ADD nest runs "B=9 C=1 F=N", and its no-size-error twin
prints nothing. A probe matrix nested every family both ways. A side effect for error recovery: a syntax error just
after an owner's fixed part now surfaces in the next element's rule, because the empty loop exits. SEARCH's KEY hint
walks up to the SEARCH (`NearestSearchBeforeWhen`, shared with its NOT AT END hint).

Goldens, `cases/14.5.3.3-Implicit-scope-termination/`: `pb3830_phrase_association` at every edition, eight legs the
outward reading would print differently; `pb3830_call_on_overflow_2023` and its 85–2014 refusal; a COBOL2796
refusal per family (SIZE ERROR in both orders, OVERFLOW, EXCEPTION, INVALID KEY, AT END, END-OF-PAGE, SEARCH's AT END);
and three COBOL2269 refusals. `ConditionalPhraseDriftTests` holds the table to Table 12, scraped from the
transcription, statement by statement at the latest edition. It also holds the owners to exactly the rules that write
`conditionalPhrases`, bare, and the phrase keywords to the one head rule. Ten CONFORMS inventory rows had their
code-locations re-sited from the deleted rules (batch `pb3830-conditional-phrases`); GAP unchanged at 31.

The first gate was red on two `GeneralFormatViolationTests` rows that pinned the bare COBOL0307 for a repeated ON
OVERFLOW and a repeated AT END; the test's own header asks a change that names the rule to update the row, and both now
pin COBOL2269. Self-review folded the two readings of a phrase's leading tokens into one
(`ConditionalPhrases.KindOfLeadingTokens`), shared `CobolErrorStrategy.LastRuleChild` instead of copying it, and
dropped an unused accessor. Gate (run `000002-20261026T192839Z`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN — … Conformance 10,369/18,884 · Unit 37,292/38,397 · Characterization 7/36 cases ran (skipped 0) in 1 of
2 leg(s) · Linux half (WSL clone of HEAD) GREEN …`.

Branch review R1-1 found the association table too coarse. It modelled each statement's formats by their phrase
brackets alone, and ignored the other elements that pick a format: READ NEXT, PREVIOUS and ADVANCING ON LOCK exist only
in §14.9.30.2 Format 1 and KEY IS only in Format 2; WRITE ADVANCING only in §14.9.51.2 Format 1; and a COMPUTE whose
right side parses as a boolean-expression is §14.9.8.2 Format 2, which prints no SIZE ERROR phrase. The review finisher
re-probed it on the merged branch: `READ RELF INVALID KEY READ IXG NEXT RECORD NOT INVALID KEY … END-READ` (and its
PREVIOUS and ADVANCING ON LOCK twins) compiled and printed nothing, because END-READ and everything after it landed in
the INVALID KEY branch. The KEY IS, WRITE ADVANCING and boolean COMPUTE nestings refused legal programs. A sweep for the
other arm found top-level `READ IXG NEXT RECORD INVALID KEY` and `READ IXG ADVANCING ON LOCK INVALID KEY` on a dynamic
indexed file compiling clean. The fix stays one decision: each `Format` carries the selectors it does not print
(`ConditionalPhraseAssociation.Selector`), and a format whose unprinted selector the statement wrote takes none of its
phrases, at every edition. The superset reading still relaxes only the edition. All six nestings now print `F=N`; the
four top-level forms are COBOL2269, and the message names the selector. Two older cases (`pb334`, `pb909`) dropped the
explicit NEXT and the INVALID KEY phrase, so they still witness their organization rule (COBOL1720) rather than the
syntax rule. `BooleanOperatorTests` now pins COBOL2269 for the boolean-expression form and COBOL1511 for the
sole-identifier form the binder re-routes. `ConditionalPhraseDriftTests` scrapes every Table 12 statement's general
formats and requires each statement whose formats print different brackets (ACCEPT, COMPUTE, DELETE, DISPLAY, READ,
WRITE) to be split by grammar rule or to carry selectors. A mutation that dropped COMPUTE's selector failed it. New
goldens: `pb3830_phrase_format_selectors` (85–2023), `pb3830_phrase_format_selectors_2002`, and four COBOL2269
refusals. The PB3830 harm flags now record the wrong answer, the legal-source rejections and the under-rejections this
landing fixed (lead R1-2).
