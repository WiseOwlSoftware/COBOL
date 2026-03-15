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
    : identifier
    ;

jsonTarget
    : identifier
    ;

jsonWithDetail
    : 'WITH' 'DETAIL'
    ;

jsonOnException
    : ON EXCEPTION imperativeStatement
      (NOT ON EXCEPTION imperativeStatement)?
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
    : identifier
    ;

jsonInput
    : identifier
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
    : identifier
    ;

xmlOnException
    : ON EXCEPTION imperativeStatement
      (NOT ON EXCEPTION imperativeStatement)?
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
    : identifier
    ;

xmlInput
    : identifier
    ;

xmlCountIn
    : 'COUNT' 'IN' identifier
    ;
