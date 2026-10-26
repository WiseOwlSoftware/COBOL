## PB3830: one grammar rule and one §14.5.3.3 decision for every paired conditional phrase

The fifth fuzz night's adjudicator called COBOLNET2796 on `SUBTRACT A FROM C ON SIZE ERROR ADD 1 TO D NOT ON SIZE
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
at 2023 and is COBOLNET2796 at 85–2014, where CALL's 1985–2014 format prints it. A superset reading keeps
`CALL … ON OVERFLOW` alone associated at 2023, so COBOLNET0882 still names the removal, but only when no enclosing
statement may take the phrase, so the superset never moves a legal program's phrase. Reading the edition there is
recorded as the one phrase-association read of `EditionReadDriftTests`. `CALL … NOT ON OVERFLOW` (no edition prints
it), `CALL … ON OVERFLOW … NOT ON EXCEPTION` and `READ … AT END … INVALID KEY` (two formats mixed) used to compile;
each is now refused COBOLNET2269 from the parser state, as is a second occurrence of a phrase. COBOLNET2796's message
quotes §14.5.3.3 when the inner statement took a phrase the outer statement's format also prints.

Re-probe: r2796 and the MULTIPLY/COMPUTE sibling are refused as before. The CALL/DISPLAY probe is legal and accepted
(device DISPLAY prints no EXCEPTION phrase). The same-verb ADD nest runs "B=9 C=1 F=N", and its no-size-error twin
prints nothing. A probe matrix nested every family both ways. A side effect for error recovery: a syntax error just
after an owner's fixed part now surfaces in the next element's rule, because the empty loop exits. SEARCH's KEY hint
walks up to the SEARCH (`NearestSearchBeforeWhen`, shared with its NOT AT END hint).

Goldens, `cases/14.5.3.3-Implicit-scope-termination/`: `pb3830_phrase_association` at every edition, eight legs the
outward reading would print differently; `pb3830_call_on_overflow_2023` and its 85–2014 refusal; a COBOLNET2796
refusal per family (SIZE ERROR in both orders, OVERFLOW, EXCEPTION, INVALID KEY, AT END, END-OF-PAGE, SEARCH's AT END);
and three COBOLNET2269 refusals. `ConditionalPhraseDriftTests` holds the table to Table 12, scraped from the
transcription, statement by statement at the latest edition. It also holds the owners to exactly the rules that write
`conditionalPhrases`, bare, and the phrase keywords to the one head rule. Ten CONFORMS inventory rows had their
code-locations re-sited from the deleted rules (batch `pb3830-conditional-phrases`); GAP unchanged at 31.
