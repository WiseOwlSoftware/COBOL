*>--- conformance-case
*> id: pb4298_floating_debugging_free
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
>>SOURCE FORMAT FREE
*> kb/Work PB4298 - the FLOATING debugging indicator >>D of ISO/IEC 1989:2002
*> (6.1.2: ">>D debugging indicator - a debugging line when followed by at
*> least one space character"; 6.1.2.1 SR7: preceded on a line only by zero
*> or more spaces; 6.3.4: "After all COPY and REPLACE statements have been
*> processed, a debugging line has the characteristics of a comment line if
*> the WITH DEBUGGING MODE clause is not specified"). ISO 2023 6.2.3.1 lets a
*> floating indicator stand in free form as in fixed form. The 2002 text is
*> not in the repository: read from the archived draft.
*> WITH DEBUGGING MODE is specified, so every debugging line is SOURCE: the
*> data item the first one declares, the statement the second one carries,
*> and the indented third one all run. Each leg can fail on its own: a >>D
*> line read as a comment drops its DISPLAY (and leaves W-DBG undeclared), a
*> >>D line read as program text is COBOL0001 "unexpected '>'".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4298FR.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-LIVE PIC X(4) VALUE "LIVE".
>>D 01 W-DBG PIC X(5) VALUE "DEBUG".
PROCEDURE DIVISION.
    DISPLAY "A".
>>D    DISPLAY W-DBG " " W-LIVE.
   >>D DISPLAY "INDENTED".
    DISPLAY "B".
    STOP RUN.
