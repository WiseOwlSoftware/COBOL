// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// REPORT SECTION rules (COBOL-85, ISO 1989:1985 §13.8/§13.14/§13.15; clauses §13.18.12/14/16/28/37/39/53/54/57).
// Imported by CobolParserCore.g4 — no options block of its own beyond tokenVocab.

parser grammar CobolReportWriter;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// REPORT SECTION
// ==========================================

reportSection
    : REPORT SECTION DOT reportDescriptionEntry*
    ;

// RD report-name [report-description-clause]... .  [report-group-entry]...
reportDescriptionEntry
    : RD reportName reportDescriptionClause* DOT reportGroupEntry*
    ;

reportName
    : cobolWord
    ;

reportDescriptionClause
    : reportGlobalClause
    | reportCodeClause
    | reportControlClause
    | reportPageClause
    ;

// IS GLOBAL (§13.18.23)
reportGlobalClause
    : IS? GLOBAL
    ;

// CODE literal (§13.18.12) — a 2-char prefix written on every line of the report.
reportCodeClause
    : CODE IS? (literal | dataReference)
    ;

// CONTROL(S) {FINAL | data-name}... (§13.18.16) — the control hierarchy (major→minor).
reportControlClause
    : (CONTROL IS? | CONTROLS ARE?) (FINAL | dataReference)+
    ;

// PAGE LIMIT IS n LINES [HEADING n] [FIRST DETAIL n] [LAST DETAIL n] [FOOTING n] (§13.18.37)
reportPageClause
    : PAGE (LIMIT IS? | LIMITS ARE?)? integerLiteral (LINE | LINES)?
      reportPageSubclause*
    ;

reportPageSubclause
    : HEADING IS? integerLiteral
    | FIRST DETAIL IS? integerLiteral
    | LAST DETAIL IS? integerLiteral
    | FOOTING IS? integerLiteral
    ;

// ==========================================
// REPORT GROUP DESCRIPTION ENTRY (§13.15)
// ==========================================

reportGroupEntry
    : levelNumber reportGroupName? reportGroupClause* DOT
    ;

reportGroupName
    : cobolWord
    ;

reportGroupClause
    : reportTypeClause
    | reportLineClause
    | reportNextGroupClause
    | reportColumnClause
    | reportSourceClause
    | reportSumClause
    | reportGroupIndicateClause
    | pictureClause
    | usageClause
    | signClause
    | justifiedClause
    | blankWhenZeroClause
    | occursClause
    | valueClause
    ;

// TYPE IS {REPORT HEADING|RH | PAGE HEADING|PH | CONTROL HEADING|CH [FINAL|data] | DETAIL|DE |
//          CONTROL FOOTING|CF [FINAL|data] | PAGE FOOTING|PF | REPORT FOOTING|RF}  (§13.18.57)
reportTypeClause
    : TYPE IS? reportGroupType
    ;

reportGroupType
    : (REPORT HEADING | RH)
    | (PAGE HEADING | PH)
    | (CONTROL HEADING | CH) (FINAL | dataReference)?
    | (DETAIL | DE)
    | (CONTROL FOOTING | CF) (FINAL | dataReference)?
    | (PAGE FOOTING | PF)
    | (REPORT FOOTING | RF)
    ;

// LINE NUMBER IS {integer [ON NEXT PAGE] | PLUS integer | NEXT PAGE}  (§13.18.28)
reportLineClause
    : LINE NUMBER? IS? (PLUSWORD integerLiteral | integerLiteral (ON? NEXT PAGE)? | NEXT PAGE)
    ;

// NEXT GROUP IS {integer | PLUS integer | NEXT PAGE}  (§13.18.39)
reportNextGroupClause
    : NEXT GROUP IS? (PLUSWORD integerLiteral | integerLiteral | NEXT PAGE)
    ;

// COLUMN NUMBER IS integer  (§13.18.14)
reportColumnClause
    : COLUMN NUMBER? IS? integerLiteral
    ;

// SOURCE IS identifier  (§13.18.53)
reportSourceClause
    : SOURCE IS? dataReference
    ;

// SUM data-name... [UPON data-name...] [RESET ON {FINAL|data-name}]  (§13.18.54)
reportSumClause
    : SUM sumOperand (COMMA? sumOperand)*
      (UPON dataReference (COMMA? dataReference)*)?
      reportSumReset?
    ;

sumOperand
    : dataReference (OF reportName)?
    ;

reportSumReset
    : RESET ON? (FINAL | dataReference)
    ;

// GROUP INDICATE  (§13.18.28 group-indicate phrase) — print this field only on the first
// detail of a page or after a control break.
reportGroupIndicateClause
    : GROUP INDICATE?
    ;

// ==========================================
// REPORT WRITER PROCEDURE-DIVISION VERBS (§14.9.x)
// ==========================================

// INITIATE report-name... (§14.9.21) — begin report processing: reset LINE-COUNTER/PAGE-COUNTER + SUM counters.
initiateStatement
    : INITIATE reportName+
    ;

// GENERATE {report-group-name | report-name} (§14.9.19) — produce a detail line (or summary reporting).
// The operand is a report-group-name (detail reporting) or a report-name (summary reporting); the binder
// distinguishes by resolving the name against the report model.
generateStatement
    : GENERATE reportName
    ;

// TERMINATE report-name... (§14.9.62) — end report processing: produce final CONTROL/REPORT FOOTINGs.
terminateStatement
    : TERMINATE reportName+
    ;
