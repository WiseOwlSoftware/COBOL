// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Extension stubs for JSON/XML/OO statements that live in CobolParserCore.
// These are overridden by CobolParserOO and CobolParserJsonXml import grammars.
// Also contains the INLINE METHOD INVOCATION (COBOL 2023) rule.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolExtensionsJsonXml;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// JSON / XML statements (introduced COBOL 2014; ISO 2023 §14.9)
// ==========================================
// SEAM-LEVEL surface (the design's scope-flag for the JSON/XML subsystem): the statement heads, COUNT phrase,
// XML PARSE's mandatory PROCESSING PROCEDURE, and the exception phrases parse; the detail phrases (NAME/TYPE/
// SUPPRESS/CONVERTING/ENCODING/VALIDATING/NAMESPACE) arrive with the subsystem wave. The binder loud-fails the
// statements BY NAME until then — but the per-edition gating ({is2014()}? in CobolParserCore) is REAL today.
// (Replaced the pre-seam placeholder 'JSON (dataReference|literal)+' stubs, which accepted no conforming program —
// the version matrix's json-generate-2014 row caught it, DEVLOG 531.)

jsonStatement
    : JSON GENERATE dataReference FROM dataReference (COUNT IN? dataReference)? jsonXmlExceptionPhrases END_JSON?
    | JSON PARSE dataReference INTO dataReference jsonXmlExceptionPhrases END_JSON?
    ;

xmlStatement
    : XML GENERATE dataReference FROM dataReference (COUNT IN? dataReference)? jsonXmlExceptionPhrases END_XML?
    | XML PARSE dataReference
      PROCESSING PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
      jsonXmlExceptionPhrases END_XML?
    ;

jsonXmlExceptionPhrases
    : (ON? EXCEPTION statementBlock)? (NOT ON? EXCEPTION statementBlock)?
    ;
// invokeStatement (OO/2002) is factored into Core/CobolOO.g4 (not here — this fragment is JSON/XML, 2014).

// ==========================================
// INLINE METHOD INVOCATION (COBOL 2023)
// ==========================================

inlineMethodInvocationStatement
    : dataReference LPAREN argumentList? RPAREN
    ;
