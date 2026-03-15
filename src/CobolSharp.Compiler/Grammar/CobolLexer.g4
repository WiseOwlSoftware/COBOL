// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

lexer grammar CobolLexer;

// ==========================================
// DEFAULT MODE
// ==========================================
// Assumes preprocessed input: fixed→free normalized, COPY/REPLACE expanded.

WS           : [ \t\r\n]+ -> skip ;
COMMENT_START: '*>' -> skip, pushMode(COMMENT_MODE) ;

// ── END-xxx paired terminators (must precede END and IDENTIFIER) ──

END_IF       : 'END-IF' ;
END_PERFORM  : 'END-PERFORM' ;
END_EVALUATE : 'END-EVALUATE' ;
END_READ     : 'END-READ' ;
END_SEARCH   : 'END-SEARCH' ;
END_CALL     : 'END-CALL' ;
END_SORT     : 'END-SORT' ;
END_MERGE    : 'END-MERGE' ;
END_RETURN   : 'END-RETURN' ;
END_REWRITE  : 'END-REWRITE' ;
END_DELETE   : 'END-DELETE' ;
END_WRITE    : 'END-WRITE' ;
END_START    : 'END-START' ;
END_INVOKE   : 'END-INVOKE' ;
END_JSON     : 'END-JSON' ;
END_XML      : 'END-XML' ;
END_METHOD   : 'END-METHOD' ;
END_ADD      : 'END-ADD' ;
END_SUBTRACT : 'END-SUBTRACT' ;
END_MULTIPLY : 'END-MULTIPLY' ;
END_DIVIDE   : 'END-DIVIDE' ;
END_COMPUTE  : 'END-COMPUTE' ;
END_STRING   : 'END-STRING' ;
END_UNSTRING : 'END-UNSTRING' ;
END_ACCEPT   : 'END-ACCEPT' ;
END_DISPLAY  : 'END-DISPLAY' ;

// ── Hyphenated keywords (must precede IDENTIFIER) ──

PROGRAM_ID      : 'PROGRAM-ID' ;
METHOD_ID       : 'METHOD-ID' ;
CLASS_ID        : 'CLASS-ID' ;
INTERFACE_ID    : 'INTERFACE-ID' ;
WORKING_STORAGE : 'WORKING-STORAGE' ;
LOCAL_STORAGE   : 'LOCAL-STORAGE' ;
NEXT_SENTENCE   : 'NEXT' [ ]+ 'SENTENCE' ;
BY_REFERENCE    : 'BY' [ ]+ 'REFERENCE' ;
BY_VALUE        : 'BY' [ ]+ 'VALUE' ;
BY_CONTENT      : 'BY' [ ]+ 'CONTENT' ;
DATE_WRITTEN    : 'DATE-WRITTEN' ;
DATE_COMPILED   : 'DATE-COMPILED' ;
SOURCE_COMPUTER : 'SOURCE-COMPUTER' ;
OBJECT_COMPUTER : 'OBJECT-COMPUTER' ;
SPECIAL_NAMES   : 'SPECIAL-NAMES' ;
FILE_CONTROL    : 'FILE-CONTROL' ;
I_O_CONTROL     : 'I-O-CONTROL' ;
PACKED_DECIMAL  : 'PACKED-DECIMAL' ;
BLANK_WHEN_ZERO : 'BLANK' [ ]+ 'WHEN' [ ]+ 'ZERO' ;

// ── Division/section keywords ──

IDENTIFICATION : 'IDENTIFICATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
SECTION     : 'SECTION' ;
LINKAGE     : 'LINKAGE' ;
FD          : 'FD' ;
SD          : 'SD' ;

// ── Statement keywords ──

ACCEPT      : 'ACCEPT' ;
ADD         : 'ADD' ;
ALTER       : 'ALTER' ;
CALL        : 'CALL' ;
CANCEL      : 'CANCEL' ;
CLOSE       : 'CLOSE' ;
COMPUTE     : 'COMPUTE' ;
CONTINUE    : 'CONTINUE' ;
DELETE      : 'DELETE' ;
DISPLAY     : 'DISPLAY' ;
DIVIDE      : 'DIVIDE' ;
EVALUATE    : 'EVALUATE' ;
EXIT        : 'EXIT' ;
GOBACK      : 'GOBACK' ;
GO          : 'GO' ;
IF          : 'IF' ;
INITIALIZE  : 'INITIALIZE' ;
INSPECT     : 'INSPECT' ;
INVOKE      : 'INVOKE' ;
JSON        : 'JSON' ;
MERGE       : 'MERGE' ;
MOVE        : 'MOVE' ;
MULTIPLY    : 'MULTIPLY' ;
OPEN        : 'OPEN' ;
PERFORM     : 'PERFORM' ;
READ        : 'READ' ;
RELEASE     : 'RELEASE' ;
RETURN      : 'RETURN' ;
REWRITE     : 'REWRITE' ;
SEARCH      : 'SEARCH' ;
SET         : 'SET' ;
SORT        : 'SORT' ;
START       : 'START' ;
STOP        : 'STOP' ;
STRING      : 'STRING' ;
SUBTRACT    : 'SUBTRACT' ;
UNSTRING    : 'UNSTRING' ;
WRITE       : 'WRITE' ;
XML         : 'XML' ;

// ── Clause/phrase keywords ──

ACCESS      : 'ACCESS' ;
ADDRESS     : 'ADDRESS' ;
ADVANCING   : 'ADVANCING' ;
AFTER       : 'AFTER' ;
ALL         : 'ALL' ;
ALTERNATE   : 'ALTERNATE' ;
AND         : 'AND' ;
ASCENDING   : 'ASCENDING' ;
ASSIGN      : 'ASSIGN' ;
AT          : 'AT' ;
AUTHOR      : 'AUTHOR' ;
BEFORE      : 'BEFORE' ;
BINARY      : 'BINARY' ;
BLANK       : 'BLANK' ;
BY          : 'BY' ;
CHARACTER   : 'CHARACTER' ;
CLASS       : 'CLASS' ;
COMMON      : 'COMMON' ;
COMP        : 'COMP' ;
COMP_1      : 'COMP-1' ;
COMP_2      : 'COMP-2' ;
COMP_3      : 'COMP-3' ;
CONTENT     : 'CONTENT' ;
CONVERTING  : 'CONVERTING' ;
CORRESPONDING : 'CORRESPONDING' ;
COUNT       : 'COUNT' ;
DECLARATIVES: 'DECLARATIVES' ;
DELIMITED   : 'DELIMITED' ;
DELIMITER   : 'DELIMITER' ;
DEPENDING   : 'DEPENDING' ;
DESCENDING  : 'DESCENDING' ;
DOWN        : 'DOWN' ;
DUPLICATES  : 'DUPLICATES' ;
DYNAMIC     : 'DYNAMIC' ;
ELSE        : 'ELSE' ;
END         : 'END' ;
EQUAL       : 'EQUAL' ;
ERROR       : 'ERROR' ;
EXCEPTION   : 'EXCEPTION' ;
EXTEND      : 'EXTEND' ;
EXTERNAL    : 'EXTERNAL' ;
FIRST       : 'FIRST' ;
FOR         : 'FOR' ;
FALSE_      : 'FALSE' ;
FILE        : 'FILE' ;
FILLER      : 'FILLER' ;
FROM        : 'FROM' ;
FUNCTION    : 'FUNCTION' ;
GENERIC     : 'GENERIC' ;
GIVING      : 'GIVING' ;
GLOBAL      : 'GLOBAL' ;
GREATER     : 'GREATER' ;
IN          : 'IN' ;
INDEXED     : 'INDEXED' ;
INITIAL_    : 'INITIAL' ;
INPUT       : 'INPUT' ;
INSTALLATION: 'INSTALLATION' ;
INTO        : 'INTO' ;
INVALID     : 'INVALID' ;
IS          : 'IS' ;
JUST        : 'JUST' ;
JUSTIFIED   : 'JUSTIFIED' ;
KEY         : 'KEY' ;
LEADING     : 'LEADING' ;
LESS        : 'LESS' ;
LINE        : 'LINE' ;
LINES       : 'LINES' ;
METHOD      : 'METHOD' ;
MODE        : 'MODE' ;
NEXT        : 'NEXT' ;
NOT         : 'NOT' ;
NULL_       : 'NULL' ;
OCCURS      : 'OCCURS' ;
OF          : 'OF' ;
ON          : 'ON' ;
OR          : 'OR' ;
ORGANIZATION: 'ORGANIZATION' ;
OTHER       : 'OTHER' ;
OUTPUT      : 'OUTPUT' ;
OVERFLOW    : 'OVERFLOW' ;
PACKED      : 'PACKED' ;
PARAGRAPH   : 'PARAGRAPH' ;
// PIC/PICTURE → push into PICMODE to capture the PIC string as one token.
// Handles: PIC X(120), PIC IS S9(18), PICTURE $$$,$$9.99CR, etc.
PIC         : ('PIC' | 'PICTURE') -> pushMode(PICMODE) ;
POINTER     : 'POINTER' ;
PREVIOUS    : 'PREVIOUS' ;
PROGRAM     : 'PROGRAM' ;
RANDOM      : 'RANDOM' ;
RECORD      : 'RECORD' ;
RECURSIVE   : 'RECURSIVE' ;
REDEFINES   : 'REDEFINES' ;
REPLACING   : 'REPLACING' ;
REFERENCE   : 'REFERENCE' ;
RELATIVE    : 'RELATIVE' ;
REMAINDER   : 'REMAINDER' ;
REMARKS     : 'REMARKS' ;
RENAMES     : 'RENAMES' ;
RETURNING   : 'RETURNING' ;
ROUNDED     : 'ROUNDED' ;
RIGHT       : 'RIGHT' ;
RUN         : 'RUN' ;
SECURITY    : 'SECURITY' ;
SELECT      : 'SELECT' ;
SELF        : 'SELF' ;
SEPARATE    : 'SEPARATE' ;
SEQUENTIAL  : 'SEQUENTIAL' ;
SIGN        : 'SIGN' ;
SIZE        : 'SIZE' ;
STATUS      : 'STATUS' ;
SUPER       : 'SUPER' ;
SYNC        : 'SYNC' ;
SYNCHRONIZED: 'SYNCHRONIZED' ;
TALLYING    : 'TALLYING' ;
THAN        : 'THAN' ;
THROUGH     : 'THROUGH' ;
THRU        : 'THRU' ;
TIMES       : 'TIMES' ;
TO          : 'TO' ;
TRAILING    : 'TRAILING' ;
TRUE_       : 'TRUE' ;
TYPE        : 'TYPE' ;
TYPEDEF     : 'TYPEDEF' ;
UNTIL       : 'UNTIL' ;
UP          : 'UP' ;
USAGE       : 'USAGE' ;
USING       : 'USING' ;
VALUE       : 'VALUE' ;
VALUES      : 'VALUES' ;
VARYING     : 'VARYING' ;
WHEN        : 'WHEN' ;
WITH        : 'WITH' ;
ZERO        : 'ZERO' | 'ZEROS' | 'ZEROES' ;
SPACE       : 'SPACE' | 'SPACES' ;
HIGH_VALUE  : 'HIGH-VALUE' | 'HIGH-VALUES' ;
LOW_VALUE   : 'LOW-VALUE' | 'LOW-VALUES' ;
QUOTE_      : 'QUOTE' | 'QUOTES' ;

// ── Numeric literals (must come BEFORE IDENTIFIER) ──
// Option A: ordering guarantees "01" → INTEGERLIT, not IDENTIFIER

DECIMALLIT  : [0-9]+ '.' [0-9]+ | '.' [0-9]+ ;
INTEGERLIT  : [0-9]+ ;

// ── IDENTIFIER (must come AFTER all keywords AND numeric literals) ──
// Option B: identifiers must start with a letter (matches COBOL spec —
// user-defined words begin with a letter, not a digit)

IDENTIFIER
    : [A-Za-z] [A-Za-z0-9-]* [A-Za-z0-9]
    | [A-Za-z]
    ;

// ── String literals ──

STRINGLIT   : '"' (~["\r\n] | '""')* '"'
            | '\'' (~['\r\n] | '\'\'')* '\''
            ;
HEXLIT      : [Xx] '"' [0-9A-Fa-f]+ '"'
            | [Xx] '\'' [0-9A-Fa-f]+ '\''
            ;

// ── Operators (multi-char before single-char) ──

POWER       : '**' ;
LTEQUAL     : '<=' ;
GTEQUAL     : '>=' ;
NOTEQUAL    : '<>' ;

DOT         : '.' ;
COMMA       : ',' -> skip ;   // §8.3.5: comma-space is equivalent to space
LPAREN      : '(' ;
RPAREN      : ')' ;
LT          : '<' ;
GT          : '>' ;
EQUALS      : '=' ;
PLUS        : '+' ;
MINUS       : '-' ;
STAR        : '*' ;
SLASH       : '/' ;
COLON       : ':' ;
SEMICOLON   : ';' -> skip ;   // §8.3.5: semicolon-space is equivalent to space

// ── Catch-all for unrecognized characters ──

ANY_CHAR    : . ;

// ==========================================
// PICMODE — captures PIC/PICTURE string as one token
// ==========================================
// After PIC/PICTURE, optionally skip IS, then capture the entire
// PIC string (e.g., X(120), S9(18), $$$,$$9.99CR) as one token.
//
// Key insight: PIC strings never contain spaces. A period within
// a PIC string (like 9.99) is always followed by another PIC char,
// while a sentence-ending period is followed by whitespace/EOF.

mode PICMODE;

PIC_IS      : 'IS' -> skip ;              // optional IS keyword
PIC_WS      : [ \t\r\n]+ -> skip ;        // skip whitespace
PIC_STRING  : ( ~[ \t\r\n.] | '.' ~[ \t\r\n] )+ -> popMode ;
    // Matches: any non-whitespace-non-period char,
    //      OR: a period followed by a non-whitespace char (embedded decimal)
    // Stops:  at whitespace or period-before-whitespace (sentence end)

// ==========================================
// COMMENT_MODE — *> to end of line
// ==========================================

mode COMMENT_MODE;

COMMENT_TEXT : ~[\r\n]+ -> skip ;
COMMENT_END  : [\r\n]   -> popMode, skip ;
