lexer grammar CobolLexer;

// ==========================================
// DEFAULT MODE (assumes preprocessed, free/fixed normalized)
// ==========================================

WS          : [ \t\r\n]+ -> skip ;
COMMENTLINE : '*>' ~[\r\n]* -> skip ;

// --- keywords (uppercased by preprocessor) ---
// Order matters: longer matches first, then keywords before IDENTIFIER

// COPY/REPLACE directives — push to COPYMODE for preprocessor capture
COPY_DIRECTIVE : 'COPY' -> pushMode(COPYMODE) ;
REPLACE_DIRECTIVE : 'REPLACE' -> pushMode(REPLACEMODE) ;

// END-xxx paired terminators (must precede END)
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

// Hyphenated keywords (must precede IDENTIFIER)
PROGRAM_ID      : 'PROGRAM-ID' ;
METHOD_ID       : 'METHOD-ID' ;
WORKING_STORAGE : 'WORKING-STORAGE' ;
LOCAL_STORAGE   : 'LOCAL-STORAGE' ;
NEXT_SENTENCE   : 'NEXT' [ ]+ 'SENTENCE' ;
BY_REFERENCE    : 'BY' [ ]+ 'REFERENCE' ;
BY_VALUE        : 'BY' [ ]+ 'VALUE' ;
BY_CONTENT      : 'BY' [ ]+ 'CONTENT' ;

// Division/section keywords
IDENTIFICATION : 'IDENTIFICATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
SECTION     : 'SECTION' ;
LINKAGE     : 'LINKAGE' ;
FILE        : 'FILE' ;
FD          : 'FD' ;

// Statement keywords
READ        : 'READ' ;
WRITE       : 'WRITE' ;
OPEN        : 'OPEN' ;
CLOSE       : 'CLOSE' ;
IF          : 'IF' ;
ELSE        : 'ELSE' ;
PERFORM     : 'PERFORM' ;
EVALUATE    : 'EVALUATE' ;
INVOKE      : 'INVOKE' ;
JSON        : 'JSON' ;
XML         : 'XML' ;
CLASS       : 'CLASS' ;
TYPEDEF     : 'TYPEDEF' ;
GENERIC     : 'GENERIC' ;
CONTINUE    : 'CONTINUE' ;

// Clause/phrase keywords
USING       : 'USING' ;
RETURNING   : 'RETURNING' ;
INTO        : 'INTO' ;
FROM        : 'FROM' ;
KEY         : 'KEY' ;
IS          : 'IS' ;
AT          : 'AT' ;
END         : 'END' ;
NOT         : 'NOT' ;
INVALID     : 'INVALID' ;
ON          : 'ON' ;
EXCEPTION   : 'EXCEPTION' ;
NEXT        : 'NEXT' ;
PREVIOUS    : 'PREVIOUS' ;
RECORD      : 'RECORD' ;

// --- IDENTIFIER (must come AFTER all keywords) ---

IDENTIFIER
    : [A-Za-z0-9] [A-Za-z0-9-]* [A-Za-z0-9]
    | [A-Za-z0-9]
    ;

// --- literals ---

INTEGERLIT  : [0-9]+ ;
DECIMALLIT  : [0-9]+ '.' [0-9]+ ;
STRINGLIT   : '"' (~["\r\n] | '""')* '"'
            | '\'' (~['\r\n] | '\'\'')* '\''
            ;
HEXLIT      : 'X' '"' [0-9A-Fa-f]+ '"'
            | 'X' '\'' [0-9A-Fa-f]+ '\''
            ;

// --- punctuation ---

DOT         : '.' ;
COMMA       : ',' ;
LPAREN      : '(' ;
RPAREN      : ')' ;
LT          : '<' ;
GT          : '>' ;
LTEQUAL     : '<=' ;
GTEQUAL     : '>=' ;
NOTEQUAL    : '<>' ;
EQUALS      : '=' ;
PLUS        : '+' ;
MINUS       : '-' ;
STAR        : '*' ;
SLASH       : '/' ;
POWER       : '**' ;
COLON       : ':' ;
SEMICOLON   : ';' ;

// ==========================================
// COPYMODE — captures COPY directive content
// ==========================================

mode COPYMODE;

COPY_WS         : [ \t\r\n]+ -> skip ;
COPY_NAME       : [A-Za-z0-9] [A-Za-z0-9-]* [A-Za-z0-9]
                | [A-Za-z0-9]
                ;
COPY_STRINGLIT  : '"' (~["\r\n])* '"'
                | '\'' (~['\r\n])* '\''
                ;
COPY_REPLACING  : 'REPLACING' ;
COPY_BY         : 'BY' ;
COPY_PSEUDO_OPEN  : '==' -> pushMode(PSEUDOTEXT) ;
COPY_DOT        : '.' -> popMode ;
COPY_TOKEN      : ~[ \t\r\n.=]+ ;

// ==========================================
// REPLACEMODE — captures REPLACE directive
// ==========================================

mode REPLACEMODE;

REPLACE_WS      : [ \t\r\n]+ -> skip ;
REPLACE_OFF     : 'OFF' ;
REPLACE_BY      : 'BY' ;
REPLACE_PSEUDO_OPEN : '==' -> pushMode(PSEUDOTEXT) ;
REPLACE_DOT     : '.' -> popMode ;
REPLACE_TOKEN   : ~[ \t\r\n.=]+ ;

// ==========================================
// PSEUDOTEXT — captures == ... == content
// ==========================================

mode PSEUDOTEXT;

PSEUDO_TEXT_CLOSE : '==' -> popMode ;
PSEUDO_TEXT_BODY  : (~[=] | '=' ~[=])+ ;
