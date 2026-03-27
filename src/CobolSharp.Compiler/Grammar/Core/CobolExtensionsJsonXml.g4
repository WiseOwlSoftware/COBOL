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
// EXTENSION STUBS (overridden by import grammars)
// ==========================================
// These are defined here as stubs so CobolParserCore compiles standalone.
// CobolParserOO, CobolParserJsonXml override them with full implementations.

jsonStatement     : JSON (dataReference | literal)+ ;
xmlStatement      : XML (dataReference | literal)+ ;
invokeStatement   : INVOKE (dataReference | literal)+ ;

// ==========================================
// INLINE METHOD INVOCATION (COBOL 2023)
// ==========================================

inlineMethodInvocationStatement
    : dataReference LPAREN argumentList? RPAREN
    ;
