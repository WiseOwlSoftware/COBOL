// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

lexer grammar CobolLexer;

options {
    caseInsensitive = true;
}

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
SENTENCE        : 'SENTENCE' ;
DATE_WRITTEN    : 'DATE-WRITTEN' ;
DATE_COMPILED   : 'DATE-COMPILED' ;
SOURCE_COMPUTER : 'SOURCE-COMPUTER' ;
OBJECT_COMPUTER : 'OBJECT-COMPUTER' ;
SPECIAL_NAMES   : 'SPECIAL-NAMES' ;
FILE_CONTROL    : 'FILE-CONTROL' ;
I_O_CONTROL     : 'I-O-CONTROL' ;
I_O             : 'I-O' ;
PACKED_DECIMAL  : 'PACKED-DECIMAL' ;
// BLANK [WHEN] ZERO is parsed as individual tokens in the parser grammar
DAY_OF_WEEK     : 'DAY-OF-WEEK' ;

// ── Division/section keywords ──

IDENTIFICATION : 'IDENTIFICATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
REPORT      : 'REPORT' ;
SECTION     : 'SECTION' ;
LINKAGE     : 'LINKAGE' ;
FD          : 'FD' ;
RD          : 'RD' ;
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
ALPHABETIC       : 'ALPHABETIC' ;
ALPHABETIC_LOWER : 'ALPHABETIC-LOWER' ;
ALPHABETIC_UPPER : 'ALPHABETIC-UPPER' ;
ADVANCING   : 'ADVANCING' ;
AFTER       : 'AFTER' ;
ALL         : 'ALL' ;
ALSO        : 'ALSO' ;
ALPHANUMERIC_EDITED : 'ALPHANUMERIC-EDITED' ;
NUMERIC_EDITED : 'NUMERIC-EDITED' ;
ALPHANUMERIC : 'ALPHANUMERIC' ;
ALTERNATE   : 'ALTERNATE' ;
AND         : 'AND' ;
ANY         : 'ANY' ;
ASCENDING   : 'ASCENDING' ;
ASSIGN      : 'ASSIGN' ;
ARE         : 'ARE' ;
AT          : 'AT' ;
AUTHOR      : 'AUTHOR' ;
BEFORE      : 'BEFORE' ;
BINARY      : 'BINARY' ;
BLANK       : 'BLANK' ;
BY          : 'BY' ;
CHARACTER   : 'CHARACTER' ;
CHARACTERS  : 'CHARACTERS' ;
CLASS       : 'CLASS' ;
COLLATING   : 'COLLATING' ;
COMMON      : 'COMMON' ;
COMP        : 'COMP' ;
COMP_1      : 'COMP-1' ;
COMP_2      : 'COMP-2' ;
COMP_3      : 'COMP-3' ;
COMP_5      : 'COMP-5' ;
COMPUTATIONAL   : 'COMPUTATIONAL' ;
COMPUTATIONAL_1 : 'COMPUTATIONAL-1' ;
COMPUTATIONAL_2 : 'COMPUTATIONAL-2' ;
COMPUTATIONAL_3 : 'COMPUTATIONAL-3' ;
COMPUTATIONAL_5 : 'COMPUTATIONAL-5' ;
CONTENT     : 'CONTENT' ;
CONVERTING  : 'CONVERTING' ;
CURRENCY    : 'CURRENCY' ;
DECIMAL_POINT : 'DECIMAL-POINT' ;
CORRESPONDING : 'CORRESPONDING' ;
COUNT       : 'COUNT' ;
DATE        : 'DATE' ;
DAY         : 'DAY' ;
DECLARATIVES: 'DECLARATIVES' ;
DELIMITED   : 'DELIMITED' ;
DELIMITER   : 'DELIMITER' ;
DEPENDING   : 'DEPENDING' ;
DESCENDING  : 'DESCENDING' ;
DOWN        : 'DOWN' ;
DUPLICATES  : 'DUPLICATES' ;
DYNAMIC     : 'DYNAMIC' ;
EDITED      : 'EDITED' ;
ELSE        : 'ELSE' ;
END         : 'END' ;
ENTRY       : 'ENTRY' ;
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
POSITIVE    : 'POSITIVE' ;
NEGATIVE    : 'NEGATIVE' ;
RESERVE     : 'RESERVE' ;
FROM        : 'FROM' ;
FUNCTION    : 'FUNCTION' ;
LABEL       : 'LABEL' ;
GENERIC     : 'GENERIC' ;
GIVING      : 'GIVING' ;
GLOBAL      : 'GLOBAL' ;
GREATER     : 'GREATER' ;
SYMBOLIC    : 'SYMBOLIC' ;
ALPHABET    : 'ALPHABET' ;
CRT         : 'CRT' ;
CURSOR      : 'CURSOR' ;
CHANNEL     : 'CHANNEL' ;
PROCEED     : 'PROCEED' ;
USE         : 'USE' ;
STANDARD    : 'STANDARD' ;
REPORTING   : 'REPORTING' ;
SUM         : 'SUM' ;
IN          : 'IN' ;
INDEX       : 'INDEX' ;
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
LEFT        : 'LEFT' ;
LESS        : 'LESS' ;
LINE        : 'LINE' ;
LINES       : 'LINES' ;
METHOD      : 'METHOD' ;
MODE        : 'MODE' ;
NEXT        : 'NEXT' ;
NOT         : 'NOT' ;
NUMERIC     : 'NUMERIC' ;
NULL_       : 'NULL' ;
OCCURS      : 'OCCURS' ;
OF          : 'OF' ;
OFF         : 'OFF' ;
ON          : 'ON' ;
OR          : 'OR' ;
OMITTED     : 'OMITTED' ;
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
RECORDS     : 'RECORDS' ;
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
SEQUENCE    : 'SEQUENCE' ;
SEQUENTIAL  : 'SEQUENTIAL' ;
SIGN        : 'SIGN' ;
SIZE        : 'SIZE' ;
STATUS      : 'STATUS' ;
SUPER       : 'SUPER' ;
SYNC        : 'SYNC' ;
SYNCHRONIZED: 'SYNCHRONIZED' ;
TALLYING    : 'TALLYING' ;
TEST        : 'TEST' ;
THAN        : 'THAN' ;
THEN        : 'THEN' ;
THROUGH     : 'THROUGH' ;
THRU        : 'THRU' ;
TIME        : 'TIME' ;
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
// DECIMALLIT handles DOT-based decimals in the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator). COMMA-based decimals for
// DECIMAL-POINT IS COMMA are handled in the parser via numericLiteralCore.

DECIMALLIT  : [0-9]+ '.' [0-9]+ | '.' [0-9]+ ;

// ── IDENTIFIER (must come BEFORE INTEGERLIT) ──
// COBOL-85 user-defined words: 1-30 chars from {A-Z, a-z, 0-9, hyphen},
// must contain at least one letter, no leading/trailing hyphen.
// Digit-start forms: 42-DATANAMES (hyphen), 11A/25COUNT/80PARTS (letter).
// Pure digits remain INTEGERLIT (level numbers, paragraph numbers, etc.).

IDENTIFIER
    : [0-9]+ '-' [a-z0-9] [a-z0-9-]*   // digit-start with hyphen: 42-DATANAMES
    | [0-9]+ [a-z] [a-z0-9-]*           // digit-start with letter: 11A, 25COUNT, 80PARTS
    | [a-z] [a-z0-9-]* [a-z0-9]         // alpha-start: WRK-DS-01V00
    | [a-z]                               // single letter: A
    ;

INTEGERLIT  : [0-9]+ ;

// ── String literals ──

STRINGLIT   : '"' (~["\r\n] | '""')* '"'
            | '\'' (~['\r\n] | '\'\'')* '\''
            ;
HEXLIT      : [x] '"' [0-9a-f]+ '"'
            | [x] '\'' [0-9a-f]+ '\''
            ;

// ── Operators (multi-char before single-char) ──

POWER       : '**' ;
LTEQUAL     : '<=' ;
GTEQUAL     : '>=' ;
NOTEQUAL    : '<>' ;

DOT         : '.' ;
// §8.3.5: comma followed by whitespace is a separator (equivalent to space).
// Comma NOT followed by whitespace is preserved for DECIMAL-POINT IS COMMA.
COMMA_SEP   : ',' [ \t\r\n]+ -> skip ;
COMMA       : ',' ;
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
