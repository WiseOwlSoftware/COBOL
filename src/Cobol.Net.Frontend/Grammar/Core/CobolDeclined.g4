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
// ⛔ EVERY RULE HERE IS EDITION-GATED AT ITS LEFT EDGE, and that is load-bearing rather than stylistic. The
// words these clauses lead with are USER-DEFINED WORDS below the edition that introduced them (§8.9:
// DEFAULT / PRESENT / VALID / VAL-STATUS / VALIDATE-STATUS from 2002; APPLY is §8.10 context-sensitive and
// never reserved; COMMIT from 2023), so an ungated arm would report "the declined VALIDATE facility" for a
// COBOL-85 program that is merely using a legal name — the wrong answer under the four-editions mandate. A
// LEFT-EDGE predicate steers ANTLR's prediction; mid-alternative it would let the alternative be chosen and
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
//    ⚠ THE §13.18.11 CLASS CLAUSE IS DELIBERATELY ABSENT. Its whole content is VALIDATE content validation
//    (§13.18.11.1), yet Annex A.4 NEVER LISTS IT — not under A.4.14, not anywhere — and it is the only
//    VALIDATE clause carrying no "obsolete feature" NOTE. If it is NOT optional, A.4.1 obliges us to ACCEPT
//    `CLASS IS NUMERIC` rather than decline it, which is a different (larger) piece of work. Adding it here
//    would silently answer an open owner question in the direction of non-support. Recorded, not decided.
// ⛔ WHERE THIS IS HOOKED IN, AND WHY THE PREDICATE IS WHERE IT IS. `CobolData.g4`'s
// `dataDescriptionClause` gains `| {is2002()}? validationClause` as its LAST alternative — recognized only
// so `DeclinedFacilityPass` can refuse the clause BY NAME with COBOLNET1708 instead of the generic parse
// error, or the misleading COBOLNET0901 "is a reserved word", that it drew before. `{is2002()}?` sits at
// the LEFT EDGE of that alternative, where a semantic predicate steers PREDICTION: mid-alternative it lets
// the alternative be chosen and then throws a raw parse error (the `left_edge_predicates` lesson). The
// gate is needed because these clause words are ordinary user-defined words at COBOL-85 (§8.9) — an
// ungated arm would answer "declined VALIDATE facility" to an '85 program that is merely naming something.
validationClause
    : validateDefaultClause
    | validateDestinationClause
    | validateInvalidClause
    | validatePresentWhenClause
    | validateVaryingClause
    | validateStatusClause
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
