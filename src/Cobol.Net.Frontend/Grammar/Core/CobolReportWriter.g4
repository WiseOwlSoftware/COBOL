// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// REPORT SECTION rules (ISO 1989:2023 §13.14/§13.15; clauses §13.18.12/14/16/29/35/37/39/41/53/54/57/64).
// The COBOL-85 surface plus the 2002 additions (PRESENT WHEN, VARYING, the multiple/relative COLUMN and
// multiple LINE operand forms) — superset parse; the 2002 forms are introduction-gated post-bind by the
// VersionConformancePass ParseArm (0900 below 2002). Imported by CobolParserCore.g4 — no options block of
// its own beyond tokenVocab.

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

// PAGE LIMIT IS n LINES [HEADING n] [FIRST DETAIL n] [LAST DETAIL n] [FOOTING n] (§13.18.39)
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
    | reportPresentWhenClause
    | reportVaryingClause
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

// {LINE|LINES} [NUMBER|NUMBERS] [IS|ARE] {integer [ON NEXT PAGE] | PLUS integer | [ON] NEXT PAGE}...  (§13.18.35 F1)
// The multi-operand form (a "multiple LINE clause", §13.18.35.3 SR10) and the LINES/NUMBERS/ARE spellings are
// COBOL-2002 — introduction-gated post-bind by VersionConformancePass ParseArm.VisitReportLineClause; the
// multi-operand repetition itself stages LOUD at bind (COBOLNET0899 report-multiple-line).
reportLineClause
    : (LINE | LINES) (NUMBER | NUMBERS)? (IS | ARE)? reportLineOperand+
    ;

reportLineOperand
    : PLUSWORD integerLiteral
    | integerLiteral (ON? NEXT PAGE)?
    | ON? NEXT PAGE
    ;

// NEXT GROUP IS {integer | PLUS integer | NEXT PAGE}  (§13.18.37)
reportNextGroupClause
    : NEXT GROUP IS? (PLUSWORD integerLiteral | integerLiteral | NEXT PAGE)
    ;

// {COLUMN|COLUMNS|COL|COLS} [NUMBER|NUMBERS] [IS|ARE] {integer | PLUS integer}...  (§13.18.14 F1)
// The multi-operand form (a "multiple COLUMN clause", §13.18.14.3 SR10), the relative PLUS operand, and the
// COL/COLS/COLUMNS/NUMBERS/ARE spellings are COBOL-2002 — introduction-gated post-bind by VersionConformancePass
// ParseArm.VisitReportColumnClause. The LEFT/CENTER/RIGHT alignment phrase has no grammar surface
// (COBOLNET_REPORT_WRITER_DESIGN §5 — the SR9 LEFT default applies).
reportColumnClause
    : (COLUMN | COLUMNS | COL | COLS) (NUMBER | NUMBERS)? (IS | ARE)? reportColumnOperand+
    ;

reportColumnOperand
    : PLUSWORD? integerLiteral
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

// GROUP INDICATE  (§13.18.29) — print this field only on the first
// detail of a page or after a control break.
reportGroupIndicateClause
    : GROUP INDICATE?
    ;

// PRESENT WHEN condition-1  (§13.18.41 Format 1, COBOL-2002) — the entry (and its subordinates) is processed
// only when the condition is true at group-presentation time. Introduction-gated post-bind by
// VersionConformancePass ParseArm.VisitReportPresentWhenClause.
reportPresentWhenClause
    : PRESENT WHEN condition
    ;

// VARYING {data-name-1 [FROM arith-1] [BY arith-2]}...  (§13.18.64, COBOL-2002) — per-repetition counters for a
// repeating report entry (a multiple LINE/COLUMN clause or a report-group OCCURS, §13.18.64.3 SR1).
// Introduction-gated post-bind by VersionConformancePass ParseArm.VisitReportVaryingClause.
reportVaryingClause
    : VARYING reportVaryingSpec+
    ;

reportVaryingSpec
    : cobolWord (FROM arithmeticExpression)? (BY arithmeticExpression)?
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

// SUPPRESS PRINTING (§14.9.45) — inhibit printing of a report group for the current instance. The statement
// carries no operand (the single fixed form); §14.9.45.3 SR1 restricts it to a USE BEFORE REPORTING procedure,
// and §14.9.45.4 GR1 fixes the affected group as the one that USE procedure names — both enforced at BIND time
// (the superset grammar admits the verb anywhere; BindSuppress resolves the lexically-enclosing declarative).
// ISO 5.2.3: measured on page 795, SUPPRESS carries an underline rule (45.3 pt) and PRINTING carries NONE, so
// PRINTING is an OPTIONAL WORD and a bare `SUPPRESS` conforms.
suppressStatement
    : SUPPRESS PRINTING?
    ;
