## Entry 3457 — 2026-11-10 19:54 PST — PB5158: at COBOL-85 a NOT conditional phrase written before its positive twin is refused (COBOL0900)

**The defect.** Every 1985 statement format with a conditional phrase pair prints the positive phrase, then the NOT
phrase, and "Clauses must be written in the sequence given in the general formats" (§1985:IV.2.1); ISO/IEC 1989:2002
Annex F.2 item 33 is the change that lets them "be specified in any order" (§2002:F.2). The compiler parses every pair
through the one `conditionalPhrases` loop, whose association never asked the edition about order, so `--std 85`
accepted `COMPUTE A = 100 NOT ON SIZE ERROR ... ON SIZE ERROR ... END-COMPUTE` silently. Re-probed on this branch's build
first: the note's repro exited 0 and displayed SIZE, and the same reversal compiled clean on all sixteen 85 owners (ADD,
SUBTRACT, MULTIPLY, DIVIDE, COMPUTE, STRING, UNSTRING, CALL ON EXCEPTION, READ AT END and INVALID KEY, RETURN, WRITE
INVALID KEY and END-OF-PAGE, REWRITE, DELETE, START).

**The fix.** The order is a fact of each general format, so it went into the one association table:
`ConditionalPhraseAssociation.Format` lists its phrases in printed order (`Printed`, with `Phrases` derived for the
membership tests), and `OutOfPrintedOrder` names the first written phrase no admitting format prints in that order. The
order binding only at 85 is an edition fact, so the refusal is a construct row, `conditional-phrases-any-order-2002`
(COBOL0900, the code every 2002 Annex F.2 relaxation uses, VCR row 7.47), checked in one place,
`VersionConformancePass.ParseArm.VisitConditionalPhrases`, which every owner's loop reaches: a statement given the rule
later inherits the check with its `Owners` row. The association itself stays order-blind, because at 85 the late phrase
can join no enclosing statement either; reading the order there would only turn the named refusal into an anonymous
syntax error (DESIGN-frontend-grammar §3.16). The message names the statement and the order: "the COMPUTE statement
writes ON SIZE ERROR after NOT ON SIZE ERROR, and its COBOL-85 general format prints ON SIZE ERROR first". The edition
gates audit proves the edge on §2023:14.9.34.3 SR4 (CHANGED from 1985). No new diagnostic code: COBOL6904-6906 returned.

**The sweep.** Five suite cases accepted at 85 wrote the reversed order, three of them 2023-rule witnesses migrated at
85 (`phrase_order_arithmetic`, `optional_on_size_error`, `phrase_order_overflow`) and two 14.5.3.3 association tests;
each now accepts from 2002 and passed at 2002, 2014 and 2023 against its unchanged `.out`. The 459 NIST programs,
checked at `--std 85` with `cobol check-batch`, draw no such diagnostic.

**Witnesses.** `ConditionalPhraseOrderTests` (each of the sixteen owners both ways: NOT first refused at 85 at the late
phrase and accepted at 2002, printed order accepted at 85); the positive `14.9.30-READ/pb5158_io_phrases_reversed`
(2002+, every input-output pair written NOT first runs its own arm); the refusal
`VI.6.8@1985-COMPUTE-STATEMENT/pb5158-compute-not-before-on-85`. Docs: VCR row 7.47, CONFORMANCE D-F2-85 (whose stale
"no 1985 text is held" is corrected), DESIGN-frontend-grammar §3.16. No inventory row is re-verdicted: the sixteen
1985 rows are 1985 rule ids that `work.py` refuses in a note (kb/Work PB5094).

