*>--- conformance-case
*> id: pb5620-write-2002-order-no-linage
*> rules: [2002-SR-14.8.47.2-18]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5620 - §2002:14.8.47.2 SR18: "If the END-OF-PAGE or the NOT END-OF-PAGE phrase is specified, the LINAGE
*> clause shall be specified in the file description entry associated with the write file." LPF has no LINAGE clause,
*> and the WRITE is in the 2002 Format 1 order (§2002:14.8.47.1: the NOT AT EOP phrase, then RETRY and WITH NO LOCK),
*> which parses at --std 2002, so the refusal is SR18's and not the parser's. Expected: COBOL0860.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5620N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT LPF ASSIGN TO "pb5620n2.prt" ORGANIZATION SEQUENTIAL
        LOCK MODE IS MANUAL.
DATA DIVISION.
FILE SECTION.
FD LPF.
01 P-REC PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT LPF
    MOVE "AAAA" TO P-REC
    WRITE P-REC AFTER ADVANCING 1 LINE
        NOT AT EOP DISPLAY "NOT EOP"
        RETRY 1 TIMES
        WITH NO LOCK
    END-WRITE
    CLOSE LPF
    STOP RUN.
