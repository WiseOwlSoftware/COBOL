lexer grammar CobolLexer;

// --- basic layout (assumes preprocessed, free/fixed normalized) ---

WS          : [ \t\r\n]+ -> skip ;
COMMENTLINE : '*' ~[\r\n]* -> skip ;
FRAGMENTDIGIT : [0-9] ;
FRAGMENTALPHA : [A-Z] ;

IDENTIFIER
    : ALNUM+ ('-' ALNUM+)*
    ;

fragment ALNUM : FRAGMENTALPHA | FRAGMENTDIGIT ;

// --- keywords (uppercased by preprocessor is easiest) ---

READ        : 'READ' ;
WRITE       : 'WRITE' ;
OPEN        : 'OPEN' ;
CLOSE       : 'CLOSE' ;
IF          : 'IF' ;
ELSE        : 'ELSE' ;
END_IF      : 'END-IF' ;
PERFORM     : 'PERFORM' ;
END_PERFORM : 'END-PERFORM' ;
EVALUATE    : 'EVALUATE' ;
END_EVALUATE: 'END-EVALUATE' ;
JSON        : 'JSON' ;
XML         : 'XML' ;
CLASS       : 'CLASS' ;
METHOD_ID   : 'METHOD-ID' ;
END_METHOD  : 'END-METHOD' ;
TYPEDEF     : 'TYPEDEF' ;
GENERIC     : 'GENERIC' ;
PROGRAM_ID  : 'PROGRAM-ID' ;
IDENTIFICATION : 'IDENTIFICATION' ;
DIVISION    : 'DIVISION' ;
ENVIRONMENT : 'ENVIRONMENT' ;
DATA        : 'DATA' ;
PROCEDURE   : 'PROCEDURE' ;
WORKING_STORAGE : 'WORKING-STORAGE' ;
LOCAL_STORAGE   : 'LOCAL-STORAGE' ;
LINKAGE     : 'LINKAGE' ;
FILE        : 'FILE' ;
SECTION     : 'SECTION' ;
FD          : 'FD' ;
END_READ    : 'END-READ' ;
END_SEARCH  : 'END-SEARCH' ;
END_CALL    : 'END-CALL' ;
END_SORT    : 'END-SORT' ;
END_MERGE   : 'END-MERGE' ;
END_RETURN  : 'END-RETURN' ;
END_REWRITE : 'END-REWRITE' ;
END_DELETE  : 'END-DELETE' ;
END_JSON    : 'END-JSON' ;
END_XML     : 'END-XML' ;
INVOKE      : 'INVOKE' ;
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
CONTINUE    : 'CONTINUE' ;
NEXT_SENTENCE : 'NEXT' ' ' 'SENTENCE' ; // or handle via parser if you prefer

// --- parameter passing conventions ---

BY_REFERENCE : 'BY' ' ' 'REFERENCE' ;
BY_VALUE     : 'BY' ' ' 'VALUE' ;
BY_CONTENT   : 'BY' ' ' 'CONTENT' ;

// --- literals ---

INTEGERLIT  : FRAGMENTDIGIT+ ;
STRINGLIT   : '"' (~["\r\n])* '"' ;

// --- punctuation ---

DOT         : '.' ;
COMMA       : ',' ;
LPAREN      : '(' ;
RPAREN      : ')' ;
LT          : '<' ;
GT          : '>' ;
EQUALS      : '=' ;
COLON       : ':' ;
SEMICOLON   : ';' ;
