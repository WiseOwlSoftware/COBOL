// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Control flow statements: PERFORM, IF, EVALUATE, GO TO, SEARCH, ALTER, USE,
// EXIT, STOP, CONTINUE, NEXT SENTENCE.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolControlFlow;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// PERFORM / END-PERFORM (§14.9.28)
// ==========================================

performStatement
    // Out-of-line: explicit forms to avoid greedy "PERFORM target" swallowing options
    : PERFORM procedureName performTimes                                       // PERFORM para N TIMES
    | PERFORM procedureName performUntil                                       // PERFORM para UNTIL cond
    | PERFORM procedureName performVarying                                     // PERFORM para VARYING ...
    | PERFORM procedureName (THRU | THROUGH) procedureName performOptions?     // PERFORM para THRU para [options]
    | PERFORM procedureName                                                    // PERFORM para (simple)
    // Inline forms
    | PERFORM performOptions+ statementBlock* END_PERFORM                 // PERFORM UNTIL/VARYING ... END-PERFORM
    | PERFORM statementBlock+ END_PERFORM                                 // PERFORM ... END-PERFORM (block)
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

performTimes
    : (integerLiteral | dataReference) TIMES
    ;

performUntil
    : (WITH? TEST (BEFORE | AFTER))? UNTIL condition
    ;

performVarying
    : (WITH? TEST (BEFORE | AFTER))?
      VARYING dataReference FROM arithmeticExpression
      (BY arithmeticExpression)?    // BY is optional per COBOL-85 spec (default = 1)
      UNTIL condition
      performVaryingAfter*
    ;

performVaryingAfter
    : AFTER dataReference FROM arithmeticExpression
      (BY arithmeticExpression)?    // BY is optional per COBOL-85 spec (default = 1)
      UNTIL condition
    ;

// ==========================================
// IF / END-IF (§14.9.19)
// ==========================================

ifStatement
    : IF condition THEN?
      statementBlock*
      (ELSE statementBlock*)?
      END_IF?
    ;

// ==========================================
// EVALUATE / END-EVALUATE (§14.9.13)
// ==========================================

evaluateStatement
    : EVALUATE evaluateSubject (ALSO evaluateSubject)*
      evaluateWhenClause+
      END_EVALUATE?
    ;

evaluateSubject
    : booleanLiteral                                     // EVALUATE TRUE / FALSE
    | valueOperand (IS? NOT? classCondition)?            // EVALUATE X [NUMERIC / class test]
    ;
    // NOTE (DEVLOG 621): an EVALUATE boolean-expression subject is STAGED RESIDUE with the condition-context
    // boolean forms (see comparisonExpression) — the boolean OPERATORS work in COMPUTE Format 2 only.

// One or more consecutive WHEN phrases share the following imperative (ISO 1989:1985
// 14.8.4): "WHEN a  WHEN b  WHEN c  imperative" executes the imperative if a, b, OR c
// matches. Each phrase is bound to its own match arm over the shared body.
evaluateWhenClause
    : evaluateWhenPhrase+ statementBlock*
    | WHEN OTHER statementBlock*
    ;

evaluateWhenPhrase
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)*
    ;

evaluateWhenGroup
    : NOT? evaluateWhenItem+
    ;

evaluateWhenItem
    : valueRange                         // WHEN A THRU N, WHEN 1 THRU 10
    | valueOperand                       // single value: "A", 1, VAR
    | condition                          // for EVALUATE TRUE / complex WHEN
    | ANY                                // match anything
    ;

// ==========================================
// GO TO (§14.9.17)
// ==========================================

goToStatement
    : GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?
    ;

// ==========================================
// SEARCH (§14.9.37 — Linear Search)
// ==========================================

searchStatement
    : SEARCH dataReference (VARYING dataReference)?
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;

searchWhenClause
    : WHEN condition statementBlock*
    ;

searchAtEndClause
    : AT END statementBlock
      (NOT AT END statementBlock)?
    | END statementBlock        // NIST / IBM extension: AT-less END
    ;

// ==========================================
// SEARCH ALL (§14.9.37 — Binary Search)
// ==========================================

searchAllStatement
    : SEARCH ALL dataReference
      searchAllKeyPhrase?
      searchAtEndClause?
      searchAllWhenClause+
      END_SEARCH?
    ;

searchAllKeyPhrase
    : KEY IS dataReference
    ;

searchAllWhenClause
    : WHEN condition statementBlock*
    ;

// ==========================================
// ALTER (§14.9.2)
// ==========================================

alterStatement
    : ALTER alterEntry+
    ;

alterEntry
    : procedureName TO (PROCEED TO)? procedureName
    ;

// ==========================================
// USE (§14.9.49, declaratives)
// ==========================================

useStatement
    // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1
    : USE GLOBAL? BEFORE REPORTING procedureName
    // Format 1: USE [GLOBAL] AFTER [STANDARD] {EXCEPTION | ERROR} PROCEDURE [ON] {file-name+ | INPUT | OUTPUT | I-O | EXTEND}
    // STANDARD and ON are accepted as optional words: the CCVS suite and mainstream compilers write
    // both "USE GLOBAL AFTER ERROR PROCEDURE ON INPUT" and "USE AFTER STANDARD ERROR PROCEDURE OUTPUT".
    | USE GLOBAL? AFTER STANDARD? (EXCEPTION | ERROR) PROCEDURE ON? useOnTarget
    // Format 3 (exception-name, EC model 2002+ — binder-gated): USE AFTER {EXCEPTION CONDITION | EC}
    // {exception-name-1 | exception-name-2 {FILE file-name-2}…}… (ISO §14.9.49.2; SR12: EC ≡ EXCEPTION
    // CONDITION). Exception-names are cobolWords — an OPEN set (EC-USER-*, §14.6.13.1.1 / §7.3.25.3 SR2), so
    // name validation (and SR13/SR14) is the binder's, never a token enumeration.
    | USE AFTER (EXCEPTION CONDITION | EC) useEcEntry+
    // Format 4 (ISO §14.9.49.2 — USE AFTER {EXCEPTION OBJECT | EO} {class-name | interface-name}, ONE
    // operand; SR15: EO ≡ EXCEPTION OBJECT): the exception-OBJECT declarative selector (GR14 — class-or-
    // subclass / IMPLEMENTS match; GR3: F4 REPLACES the F1/F3 tiers for object raises). EC-OO wave.
    | USE AFTER (EXCEPTION OBJECT | EO) cobolWord
    // X3.23-1985 debug-module format (obsolete '85 element DELETED by ISO 2002 — the whole facility,
    // DEBUG-* registers included, is absent from the 2023 text): USE FOR DEBUGGING ON {cd-name-1 |
    // [ALL REFERENCES OF] identifier-1 | file-name-1 | procedure-name-1 | ALL PROCEDURES}… .
    // Accepted-inert at 85 (the section is compiled as if comment lines — the conforming posture when
    // WITH DEBUGGING MODE is absent, and our permanently-off object-time switch when present); the
    // EditionValidator flags it COBOLNET0902 ≥2002 (`use-for-debugging-removed-2002`, VCR Table 7
    // row 7.17). ON is written by every CCVS-85 witness but accepted as optional (house optional-word
    // tolerance, cf. Format 1's ON).
    | USE FOR DEBUGGING ON? useDebugTarget+
    ;

// One '85 debug-operand: ALL PROCEDURES / [ALL REFERENCES OF] identifier / a bare name (file-name,
// procedure-name, cd-name, or unqualified identifier — the binder never distinguishes: the whole
// section is inert). dataReference covers the OF/IN-qualified identifier forms (DB201A writes
// `ABC1 OF AB2 OF A1`). The ALL-led alternatives precede the bare form (first-alternative-wins).
useDebugTarget
    : ALL PROCEDURES
    | ALL REFERENCES OF? dataReference
    | dataReference
    ;

// One Format-3 scope entry: an exception-name, optionally file-scoped ({FILE file-name-2}… — each file carries
// its own FILE word per the §14.9.49.2 format figure; SR13 requires an EC-I-O name when FILE is given).
useEcEntry
    : cobolWord (FILE fileName)*
    ;

useOnTarget
    : INPUT                     // all files opened for INPUT
    | OUTPUT                    // all files opened for OUTPUT
    | I_O                       // all files opened for I-O
    | EXTEND                    // all files opened for EXTEND
    | fileName+                 // specific file name(s)
    ;

// ==========================================
// EXIT (§14.9.14)
// ==========================================

exitStatement
    // EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2 — the RAISING tail re-raises in the activator; archaic-
    // flagged in 2023, parsed at every edition and binder-gated). The METHOD/FUNCTION forms share the tail
    // syntactically (Formats 3/4); their semantics land with the OO wave.
    : EXIT ( PROGRAM raisingPhrase? | PERFORM CYCLE? | SECTION | PARAGRAPH | METHOD raisingPhrase? | FUNCTION raisingPhrase? )?
    ;

// ==========================================
// STOP (§14.9.42)
// ==========================================

stopStatement
    : STOP RUN (stopStatusPhrase)?   // status phrase introduction-gated at BIND time (StatementBinder.BindStop → Check(StopRunStatus2002))
    | STOP literal                     // STOP literal (Format 2, obsolete)
    ;

stopStatusPhrase
    : WITH (ERROR | NORMAL) (STATUS (dataReference | literal))?   // WITH {ERROR|NORMAL} [STATUS {id|lit}]
    | STATUS (dataReference | literal)                             // STATUS {id|lit} (without WITH)
    ;

// ==========================================
// CONTINUE / NEXT SENTENCE (§14.9.9, §14.9.19)
// ==========================================

continueStatement
    : CONTINUE
    ;

nextSentenceStatement
    : NEXT SENTENCE
    ;
