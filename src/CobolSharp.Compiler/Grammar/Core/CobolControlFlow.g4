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

evaluateWhenClause
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)* statementBlock*
    | WHEN OTHER statementBlock*
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
    // Format 1: USE [GLOBAL] AFTER STANDARD {EXCEPTION | ERROR} PROCEDURE ON {file-name+ | INPUT | OUTPUT | I-O | EXTEND}
    | USE GLOBAL? AFTER STANDARD (EXCEPTION | ERROR) PROCEDURE ON useOnTarget
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
    : EXIT ( PROGRAM | PERFORM CYCLE? | SECTION | PARAGRAPH | METHOD | FUNCTION )?
    ;

// ==========================================
// STOP (§14.9.42)
// ==========================================

stopStatement
    : STOP RUN stopStatusPhrase?
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
