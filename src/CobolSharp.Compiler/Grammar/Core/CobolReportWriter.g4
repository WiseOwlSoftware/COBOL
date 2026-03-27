// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// REPORT SECTION rules (COBOL-85).
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolReportWriter;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// REPORT SECTION (COBOL-85)
// ==========================================

reportSection
    : REPORT SECTION DOT reportDescriptionEntry*
    ;

reportDescriptionEntry
    : RD reportName reportDescriptionClauses? DOT reportGroupEntry*
    ;

reportName
    : IDENTIFIER
    ;

reportDescriptionClauses
    : reportDescriptionClause+
    ;

// For now, accept standard/vendor clauses generically; binder interprets.
reportDescriptionClause
    : genericClause
    ;

reportGroupEntry
    : levelNumber reportGroupName? reportGroupBody DOT
    ;

reportGroupName
    : IDENTIFIER
    ;

reportGroupBody
    : reportGroupClause*
    ;

reportGroupClause
    : reportTypeClause
    | reportSumClause
    | genericReportGroupClause
    ;

// TYPE DETAIL / TYPE CONTROL FOOTING / TYPE PAGE HEADING / etc.
reportTypeClause
    : TYPE IDENTIFIER (IDENTIFIER)*
    ;

// SUM data-name [OF report-name] [, data-name [OF report-name]]...
reportSumClause
    : SUM sumItem (COMMA sumItem)*
    ;

sumItem
    : dataReference (OF reportName)?
    ;

genericReportGroupClause
    : genericClause
    ;
