// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// ══════════════════════════════════════════════════════════════════════════════════════════════════════════
// THE DECLINED-OPTIONAL-ELEMENT SURFACE (Annex A.4) — recognized so it can be NAMED, never implemented.
//
// Annex A.4.1: "An implementation shall accept the syntax and provide the functionality for an optional
// element only when support for that language element is claimed by the implementor." COBOL.NET claims no
// support for the VALIDATE facility (A.4.14) or for commit and rollback (A.4.3) — docs/CONFORMANCE.md §5 —
// so their syntax is REFUSED. The obligation this file discharges is that the refusal NAME THE FACILITY.
// MEASURED before this file existed, at EVERY edition: `05 A PIC X(4) DEFAULT IS "AB".` →
// `COBOL0001: no viable alternative at input 'DEFAULT'`; `05 A PIC X(4) DESTINATION IS B.` → the same, on
// DESTINATION; `05 A PIC 9(4) INVALID WHEN A = 0.` → `COBOL0307: unexpected 'INVALID'`. A user learned their
// syntax was bad, not that this implementation does not provide the facility.
// ⚠ THE NEIGHBOURING HAZARD IS A DIFFERENT POSITION, and these rules must not disturb it: `01 DESTINATION PIC
// X.` — the word in a NAME slot — draws COBOLNET0901 "'DESTINATION' is a reserved word", which is CORRECT
// there and has to keep firing. That is why every new token is admitted to `cobolWord` and why
// `conformance:negative/declined-validate-entry-name-still-0901` exists. (A pre-landing analysis attributed
// that 0901 to the CLAUSE position; the measurement says otherwise, and the truth was worse — a wholly
// generic error.) That is the same gap kb/Work PB100 closed for the locale module and kb/Work PB137 / Wave H
// closed for the VALIDATE, COMMIT and ROLLBACK *statements*; this file closes it for the DATA DIVISION and
// I-O-CONTROL halves, which is where 17 of A.4.3's 25 conditioned rules and 56 of A.4.14's 73 live.
//
// ⛔ THE PARSE IS DELIBERATELY PERMISSIVE INSIDE THE DECLINED CONSTRUCT, EXACT AT ITS EDGES. §4.2.6 excuses
// an implementation from diagnosing syntax errors WITHIN syntax it does not support, but nothing excuses it
// from leaving the surrounding program parseable — so every rule here consumes its operands for real and
// terminates on a token that cannot continue it, and none of them swallows to the period. The corollary is
// that these rules do NOT enforce the declined clauses' own syntax rules (e.g. §13.18.62.3 SR7's subscript
// arithmetic): those rules are optional WITH the facility (A.4.1), and a second, unreachable enforcement of
// them would be code nothing can ever exercise.
//
// ⛔ EVERY RULE HERE IS EDITION-GATED AT ITS LEFT EDGE, and that is load-bearing rather than stylistic —
// but for TWO different reasons, and conflating them is how a gate gets removed as redundant.
//   (a) MOST of these words are USER-DEFINED WORDS below the edition that introduced them (§8.9: DEFAULT /
//       PRESENT / VALID / VAL-STATUS / VALIDATE-STATUS from 2002; APPLY is §8.10 context-sensitive and never
//       reserved; COMMIT from 2023), so an ungated arm would report "the declined VALIDATE facility" for a
//       COBOL-85 program that is merely using a legal name — the wrong answer under the four-editions mandate.
//   (b) CLASS IS NOT ONE OF THEM. MEASURED against tests/version-matrix/reserved-words.json, CLASS is
//       reserved at ALL FOUR editions ("continuous since 1985"), because §12.3.7's SPECIAL-NAMES CLASS clause
//       has always existed. Its gate carries a different fact: the DATA-DESCRIPTION CLASS clause (§13.18.11)
//       arrived with VALIDATE at COBOL-2002, so at --std 85 `05 A PIC X CLASS IS NUMERIC.` is not a declined
//       optional element — it is a construct that does not exist in that edition, and the honest answer is a
//       syntax error, not the name of a facility. (kb/Work PB375 stated (a) for CLASS; the measurement says
//       (b). The gate is right either way; the REASON is not interchangeable.)
// A LEFT-EDGE predicate steers ANTLR's prediction; mid-alternative it would let the alternative be chosen and
// then throw (DEVLOG: the SET regression).
// ══════════════════════════════════════════════════════════════════════════════════════════════════════════

parser grammar CobolDeclined;

options {
    tokenVocab = CobolLexer;
}

// ── A.4.14 item list, the DATA DIVISION half: the §13.16.2 "validation-clauses" group of the data
//    description entry, plus the §13.18.63 format-5 content-validation entry (hooked into valueClause,
//    not here, because it is a TAIL of the VALUE clause rather than a clause of its own).
//
//    ⚖ THE §13.18.11 CLASS CLAUSE IS HERE BY OWNER DECISION, 2026-09-02 (kb/Work PB375) — it is DECLINED
//    WITH VALIDATE. It was held out until then because Annex A.4 NEVER LISTS IT — not under A.4.14, not
//    anywhere — and it is the only clause of the group carrying no "obsolete feature" NOTE, so two readings
//    were open and neither is decidable from the text: if the clause is NOT optional, A.4.1 obliges us to
//    ACCEPT `CLASS IS NUMERIC`, and declining it would reject legal source. The owner decided the other way,
//    on the ground the §13.16.2 group itself supplies: the printed Format-1 `validation-clauses` block opens
//    with `[ class-clause ]` and its meta-language table maps class-clause → "13.18.11, CLASS clause"
//    (RENDERED, PDF p394 / folio 364 — not read from the OCR alone), and §13.18.11.1 gives the clause no
//    content outside the module: "The CLASS clause specifies a range of values for each character of a data
//    item, to be checked during the content validation stage of the execution of a VALIDATE statement."
//    A.4.1's second sentence carries the licence to an optional element's unlisted associated rules.
// ⛔ THE OPERAND ALTERNATION IS ITS OWN RULE, AND THAT IS LOAD-BEARING, NOT COSMETIC. DeclinedFacilityPass
// names a declined clause by its LEADING TERMINAL RUN — the terminals before the first sub-rule — which is
// what makes a new alternative here need no code at all. CLASS is the first clause of the group whose
// operands are RESERVED WORDS rather than sub-rules, so inlining `CLASS IS? (NUMERIC | ALPHABETIC | …)`
// would make the derived name "CLASS NUMERIC" / "CLASS ALPHABETIC" — a diagnostic that renames the clause
// after whatever the user wrote. A sub-rule is also the faithful transcription: the printed format's braces
// ARE a group, exactly as validateVaryingSpec and validateStatusStage already are.
// `DeclinedFacilityTests.ClassClause_IsNamedByItsClauseWord_NotByItsOperand` pins the consequence.
// ⛔ WHERE THIS IS HOOKED IN, AND WHY THE PREDICATE IS WHERE IT IS. `CobolData.g4`'s
// `dataDescriptionClause` gains `| {is2002()}? validationClause` as its LAST alternative — recognized only
// so `DeclinedFacilityPass` can refuse the clause BY NAME with COBOLNET1708 instead of the generic parse
// error, or the misleading COBOLNET0901 "is a reserved word", that it drew before. `{is2002()}?` sits at
// the LEFT EDGE of that alternative, where a semantic predicate steers PREDICTION: mid-alternative it lets
// the alternative be chosen and then throws a raw parse error (the `left_edge_predicates` lesson). WHY the
// gate is needed is the file header's two-reason split — (a) for the clause words that are ordinary
// user-defined words at COBOL-85 (§8.9), (b) for CLASS, which is reserved at every edition and whose
// DATA-DESCRIPTION clause simply does not exist below 2002. Stated once, there; do not restate it here.
validationClause
    : validateClassClause
    | validateDefaultClause
    | validateDestinationClause
    | validateInvalidClause
    | validatePresentWhenClause
    | validateVaryingClause
    | validateStatusClause
    ;

// ISO §13.18.11.2 — CLASS IS { NUMERIC | ALPHABETIC | ALPHABETIC-LOWER | ALPHABETIC-UPPER | BOOLEAN |
//                              alphabet-name-1 | class-name-1 }
// RENDERED (PDF p412 / folio 382): CLASS and the five class-name keywords are underlined; IS is not (§5.2.3
// optional word); the braces are a plain required choice with NO choice indicators — exactly one alternative.
// ⚠ BOOLEAN HAS NO LEXER TOKEN (see CobolData.g4 initializeCategory: "BOOLEAN, DATA-POINTER, … require lexer
// tokens not yet defined"), so it arrives as IDENTIFIER and is admitted by the class-name-1 arm. That is the
// declared posture of this file — permissive INSIDE the declined construct, exact at its edges (§4.2.6) — and
// it costs nothing: the clause is refused as a whole whichever arm matched, so no operand distinction is
// observable. Adding a BOOLEAN token to serve a construct we decline would edition-gate a word across USAGE
// and the class condition for no behavioural gain.
validateClassClause
    : CLASS IS? validateClassOperand
    ;

// The printed brace group. alphabet-name-1 and class-name-1 are user-defined words (§8.9 reserves neither),
// so they ride cobolWord; the five keywords are reserved at every edition the clause exists in and need
// explicit arms because cobolWord does not admit them. Order follows the printed column.
validateClassOperand
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | cobolWord
    ;

// ISO §13.18.17.2 — DEFAULT IS { literal-1 | identifier-1 | NONE }. NONE is a §8.10 context-sensitive word
// ("DEFAULT clause") and needs no token of its own: it rides dataReference, and the binder names the clause
// either way. IS is not underlined in the printed format (an optional word, §5.2.3).
validateDefaultClause
    : DEFAULT IS? (literal | dataReference)
    ;

// ISO §13.18.18.2 — DESTINATION IS { identifier-1 } … (DESTINATION underlined, IS not).
validateDestinationClause
    : DESTINATION IS? dataReference+
    ;

// ISO §13.18.31.2 — INVALID WHEN condition-1 (both words underlined). §13.16.2 prints this clause inside a
// `{ … } …` repetition group, so an entry may carry several.
validateInvalidClause
    : INVALID WHEN condition
    ;

// ISO §13.18.41.2 FORMAT 2 (validation) — PRESENT WHEN condition-2. Format 1 (report-writer) has the SAME
// spelling and is SUPPORTED (Annex A.4.11 item 14; §5 records report writer as Partial with it implemented):
// the two are told apart by WHERE they are written, not by how, so format 1 keeps its own rule
// (reportPresentWhenClause, CobolReportWriter.g4) and this arm reaches only data description entries.
validatePresentWhenClause
    : PRESENT WHEN condition
    ;

// ISO §13.18.64.2 — VARYING { data-name-1 [ FROM arith-1 ] [ BY arith-2 ] } … The report-writer leg is the
// same shape and IS supported (A.4.11 item 20 / reportVaryingClause); same split as PRESENT WHEN.
validateVaryingClause
    : VARYING validateVaryingSpec+
    ;

validateVaryingSpec
    : cobolWord (FROM arithmeticExpression)? (BY arithmeticExpression)?
    ;

// ISO §13.18.62.2 — { VALIDATE-STATUS | VAL-STATUS } IS { identifier-1 | literal-1 }
//                   WHEN { ERROR | NO ERROR } [ ON { |FORMAT| |CONTENT| |RELATION| } ] FOR { identifier-2 } …
// §13.18.62.3 SR9: the two leading words are EQUIVALENT. The printed ON group carries CHOICE INDICATORS
// (§5.2.6.4 — one or more, each at most once, in any order), which is why the stage list is a repetition
// rather than a single alternation. FORMAT and RELATION need no dedicated tokens: FORMAT is §8.9-reserved
// from 2002 but has no lexer token, RELATION is §8.10 context-sensitive ("VALIDATE-STATUS clause"), so both
// arrive as cobolWord; CONTENT already has one. The loop terminates on FOR, a hard token.
validateStatusClause
    : (VALIDATE_STATUS | VAL_STATUS) IS? (literal | dataReference)
      WHEN NO? ERROR (ON validateStatusStage+)? FOR dataReference+
    ;

validateStatusStage
    : CONTENT
    | cobolWord
    ;

// ISO §13.18.63.2 FORMAT 5 (content-validation-entry) — the tail that turns a level-88 VALUE list into a
// content-validation entry: [ IS | ARE ] { INVALID | VALID } [ WHEN condition-1 ]. Hooked onto valueClause's
// condition-name arm, so the literal/THRU list ahead of it is the arm the supported formats 1/3/4 already
// parse. §13.16.2 Format 4 (`88 [ condition-name-2 ] value-clause .`) makes the condition-name OPTIONAL in
// this format; the entry-name slot is already optional in dataDescriptionEntry, so no change is needed for it.
validateValidPhrase
    : (IS | ARE)? (VALID | INVALID) (WHEN condition)?
    ;

// ── A.4.3 item 2: the I-O-CONTROL paragraph's APPLY COMMIT clause.
// ISO §12.4.6.3.2 — APPLY COMMIT ON [ [ file-name-1 ] [ identifier-1 ] ] …
// APPLY, COMMIT and ON are all underlined (required words). The operand list is a repetition of an
// all-optional pair, i.e. syntactically any mix of file-names and identifiers; both arrive as dataReference
// here (file-name-1 and identifier-1 are indistinguishable without the symbol table, and §12.4.6.3.3 SR1-SR4,
// which would tell them apart, are optional WITH the declined module).
applyCommitClause
    : APPLY COMMIT ON dataReference+
    ;
