## Entry 2868 — 2026-10-31 12:30 PDT — PB4206: a reserved verb written as a procedure-name is named by COBOL0901, in every procedure-name slot

**The defect.** `PERFORM RECEIVE.` at --std 2023 drew `COBOL0001: unexpected '.'` and never COBOL0901, although ISO
§8.3.2.1 1) says "Reserved words shall not be used as user-defined words or system-names" and a procedure-name is a
user-defined word (§8.3.2.2; §14.4.1 "A procedure-name is a word used to refer to a paragraph or section"). The re-probe
reproduced it and the sweep widened it: every PERFORM format (`PERFORM SEND.`, `PERFORM VALIDATE.` from 2002, `... 3
TIMES`, `... UNTIL`, `... VARYING`, `PERFORM RECEIVE THRU P1`, `PERFORM RECEIVE OF S1`) and both GO TO formats (`GO TO
RECEIVE.`, `GO TO SEND.`, `GO TO P1 RECEIVE DEPENDING ON N`) drew a COBOL0001 somewhere after the word. The slots where
nothing else can read the word (`PERFORM P1 THRU RECEIVE`, ALTER, RESUME AT, SORT INPUT PROCEDURE) already drew the
COBOL0901.

**The mechanism.** PB693 put the reservation gate's `reservedGatedWord` alternative on the procedure-name DEFINITION
wrappers (`sectionName`, `paragraphName`) and kept it off `procedureName`, so a reference could not match a gated word.
RECEIVE, SEND and VALIDATE begin statements, so where the format offers another reading the parse took it: the inline
PERFORM (whose RECEIVE statement then refused the period), or the 1985 target-less GO TO followed by a SEND statement. The
error fell after the word, where the listener's §8.9 re-coding never sees it.

**The fix.** `procedureName` takes `reservedGatedWord` in its head and its qualifier, the one rule every procedure-name
slot shares, and the definition wrappers drop their now-redundant copy. Where §8.9 reserves the word the parse succeeds and
`VersionConformancePass.VisitReservedGatedWord` names it; where §8.9 leaves it free (RECEIVE and SEND at 2014, GOBACK and
VALIDATE at 85) the match is the user-word reading the token-level gate retypes the word for, the same evidence the gate's
second witness takes from a syntax error at the word. The full-context prediction keeps every reading that parses: the
inline PERFORM is the earlier alternative, so `PERFORM GOBACK END-PERFORM` and `PERFORM VALIDATE W END-PERFORM` still bind
as inline PERFORMs, and the only procedure-name lists (GO TO DEPENDING's, ALTER's entries) end at DEPENDING or TO, which no
statement's verb supplies, so the swallow PB693 feared cannot happen. The generator's comment on `reservedGatedWord`, the
`gatedDeclaration` and `VisitReservedGatedWord` docs, the `ReservationGateRewriter` summary and
DESIGN-frontend-grammar.md's "token-level reservation gate" now say "user-defined-word slot" where they said "definition
slot".

**One judgment call.** With no procedure named by the word, the program also breaks §14.9.28.3 SR12 (§8.4.2.1 for GO TO),
and the binder's COBOL1639 now stands beside the COBOL0901: two rules, both true, the posture VersionConformancePass's parse
arm already documents. A first attempt suppressed the COBOL1639 in `ResolveProcedureOperand`; the binder's refusal ledger
then raised COBOL2362 (the edition pass reports the 0901 after the bind), and moving the §8.9 report into the binder would
have split the one reporter in two, so the suppression was dropped. With the procedure defined, the source has one fault
and draws exactly one diagnostic.

**Witnesses.** `ReservedWordPositionConformanceTests.AReservedVerbWrittenAsAProcedureName_IsNamedByCobol0901` (13 rows,
each run with the procedure defined and not); goldens `cases/8.3.2-COBOL-words/pb4206-perform-reserved-verb-as-procedure-name`
and `pb4206-go-to-reserved-verb-as-procedure-name` (refused at 85, 2002 and 2023 with COBOL0901), and
`pb4206_free_verbs_as_procedure_names` (accepted at 2014, where §8.9 leaves RECEIVE and SEND free; expected output from
§14.9.28.4 and §14.9.17.4 GR2). No inventory row changes: SR-8.3.2.1-1 already conforms (`closes_rows: []`).

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 000001-20261031T185850Z: Conformance 10,431, Unit 37,624, Characterization 7 cases in leg 1; Linux half GREEN); all 13 rows of the new theory and the 7 golden cases passed in leg 1.

