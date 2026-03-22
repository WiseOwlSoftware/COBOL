// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserJsonXml;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// ==========================================
// JSON STATEMENTS
// ==========================================

jsonStatement
    : jsonParseStatement
    | jsonGenerateStatement
    ;

// ---------- JSON PARSE ----------

jsonParseStatement
    : JSON 'PARSE' jsonSource
      INTO jsonTarget
      jsonWithDetail?
      jsonOnException?
      END_JSON?
      DOT?
    ;

jsonSource
    : dataReference
    ;

jsonTarget
    : dataReference
    ;

jsonWithDetail
    : 'WITH' 'DETAIL'
    ;

jsonOnException
    : ON EXCEPTION statementBlock
      (NOT ON EXCEPTION statementBlock)?
    ;

// ---------- JSON GENERATE ----------

jsonGenerateStatement
    : JSON 'GENERATE' jsonOutput
      FROM jsonInput
      jsonSuppressSpaces?
      jsonOnException?
      END_JSON?
      DOT?
    ;

jsonOutput
    : dataReference
    ;

jsonInput
    : dataReference
    ;

jsonSuppressSpaces
    : 'SUPPRESS' 'SPACES'
    ;

// ==========================================
// XML STATEMENTS
// ==========================================

xmlStatement
    : xmlParseStatement
    | xmlGenerateStatement
    ;

// ---------- XML PARSE ----------

xmlParseStatement
    : XML 'PARSE' xmlSource
      'PROCESSING' PROCEDURE IS procedureName
      xmlOnException?
      END_XML?
      DOT?
    ;

xmlSource
    : dataReference
    ;

xmlOnException
    : ON EXCEPTION statementBlock
      (NOT ON EXCEPTION statementBlock)?
    ;

// ---------- XML GENERATE ----------

xmlGenerateStatement
    : XML 'GENERATE' xmlOutput
      FROM xmlInput
      xmlCountIn?
      xmlOnException?
      END_XML?
      DOT?
    ;

xmlOutput
    : dataReference
    ;

xmlInput
    : dataReference
    ;

xmlCountIn
    : 'COUNT' 'IN' dataReference
    ;