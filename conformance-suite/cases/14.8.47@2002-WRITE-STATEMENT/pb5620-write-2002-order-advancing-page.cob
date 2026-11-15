*>--- conformance-case
*> id: pb5620-write-2002-order-advancing-page
*> rules: [2002-SR-14.8.47.2-17]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5620 - §2002:14.8.47.2 SR17: "The phrases ADVANCING PAGE and END-OF-PAGE shall not both be specified in
*> a single WRITE statement." The WRITE is in the 2002 Format 1 order (§2002:14.8.47.1: the END-OF-PAGE phrase, then
*> WITH LOCK), which parses at --std 2002, so the refusal is SR17's and not the parser's. Expected: COBOL0861.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5620N1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT LPF ASSIGN TO "pb5620n1.prt" ORGANIZATION SEQUENTIAL
        LOCK MODE IS MANUAL.
DATA DIVISION.
FILE SECTION.
FD LPF LINAGE IS 4 LINES.
01 P-REC PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT LPF
    MOVE "AAAA" TO P-REC
    WRITE P-REC AFTER ADVANCING PAGE
        AT END-OF-PAGE DISPLAY "EOP"
        WITH LOCK
    END-WRITE
    CLOSE LPF
    STOP RUN.
