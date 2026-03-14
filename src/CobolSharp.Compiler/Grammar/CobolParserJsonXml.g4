parser grammar CobolParserJsonXml;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// --- JSON ---

jsonStatement
    : jsonParseStatement
    | jsonGenerateStatement
    ;

jsonParseStatement
    : JSON 'PARSE' identifier
      'INTO' identifier
      ('WITH' 'DETAIL')?
      (ON EXCEPTION imperativeStatement)?
      (NOT ON EXCEPTION imperativeStatement)?
      END_JSON?
      DOT?
    ;

jsonGenerateStatement
    : JSON 'GENERATE' identifier
      'FROM' identifier
      ('SUPPRESS' 'SPACES')?
      (ON EXCEPTION imperativeStatement)?
      (NOT ON EXCEPTION imperativeStatement)?
      END_JSON?
      DOT?
    ;

// --- XML ---

xmlStatement
    : xmlParseStatement
    | xmlGenerateStatement
    ;

xmlParseStatement
    : XML 'PARSE' identifier
      'PROCESSING' 'PROCEDURE' IS procedureName
      (ON EXCEPTION imperativeStatement)?
      (NOT ON EXCEPTION imperativeStatement)?
      END_XML?
      DOT?
    ;

xmlGenerateStatement
    : XML 'GENERATE' identifier
      'FROM' identifier
      ('COUNT' 'IN' identifier)?
      (ON EXCEPTION imperativeStatement)?
      (NOT ON EXCEPTION imperativeStatement)?
      END_XML?
      DOT?
    ;
