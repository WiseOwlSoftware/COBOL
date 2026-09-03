*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 Annex A.4.8 item 1). The third FORMAT spelling: `DATA` is NOT underlined in 13.18.24.2's
*> printed general format, so it is an OPTIONAL WORD and `FORMAT NUMERIC` alone is legal source. The pair
*> with a48-format-clause-declined (which writes DATA) is what keeps the recognizer from making the word
*> required - the underlining decides required-vs-optional, and getting it backwards rejects legal COBOL
*> with a parse error instead of the named A.4.8 refusal.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48FMTN9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "a48fmtn9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT
    RECORD IS VARYING IN SIZE FROM 1 TO 80 CHARACTERS
    FORMAT NUMERIC.
01 F-REC PIC X(80).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(80) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT.
    WRITE F-REC FROM WS-REC.
    CLOSE F-OUT.
    STOP RUN.
