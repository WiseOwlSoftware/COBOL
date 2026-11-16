*>--- conformance-case
*> id: pb5620_write_eop_before_lock_2002
*> rules: [2002-SR-14.8.47.2-2, 2002-SR-14.8.47.2-18, 2002-SR-14.8.47.2-19]
*> accept: [2002]
*> constructs: [write-lock-phrases-after-end-of-page-2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5620 - the 2002 Format 1 (sequential) WRITE writes its END-OF-PAGE phrases BEFORE [ retry-phrase ] and
*> WITH LOCK / WITH NO LOCK (§2002:14.8.47.1, rendered PDF page 580); 2014 moved the pair before them (§2014:E.2
*> "Through an editorial error, the LOCK and RETRY phrases were placed in an incorrect location in the syntax diagram
*> for the sequential format of the WRITE statement"). Every WRITE here is in the 2002 order, so each leg fails if
*> that order does not parse (it was COBOL2269 at --std 2002) or binds its phrases to the wrong statement.
*> THE RULES (cite.py --check, each OK):
*>   §2002:14.8.47.2 SR2 "If the organization of the write file is sequential, format 1 shall be specified."
*>   §2002:14.8.47.2 SR18 "If the END-OF-PAGE or the NOT END-OF-PAGE phrase is specified, the LINAGE clause shall be
*>     specified in the file description entry associated with the write file." - LPF has one, so each compiles.
*>   §2002:14.8.47.2 SR19 "The words END-OF-PAGE and EOP are equivalent." - WRITE 1 spells END-OF-PAGE, 2 and 3 EOP.
*> EXPECTED (§2002:13.16.32.3 GR7 c) and d): LINAGE-COUNTER is one after OPEN OUTPUT and is incremented by the
*> ADVANCING count, or by one when ADVANCING is not specified; §2002:14.8.47.3 GR22 a): the end-of-page condition
*> occurs when the lines written do not fit within the page body, and the record is then presented on the first line
*> of the next logical page, so LINAGE-COUNTER is one again, GR7 c) 4.):
*>   WRITE 1, AFTER 1 LINE on a 5-line page body: line 2 fits, so the NOT AT END-OF-PAGE imperative runs
*>     (GR24) and the AT END-OF-PAGE one does not. LINAGE-COUNTER 2, I-O status 00.
*>   WRITE 2, AFTER 9 LINES: line 11 does not fit, so the AT EOP imperative runs and NOT AT EOP is ignored (GR23 b),
*>     written after it (any order, §2002:F.2 item 33). LINAGE-COUNTER 1, I-O status 00. RETRY and WITH NO LOCK
*>     follow the phrases, as printed.
*>   WRITE 3, FILE LPF FROM W-LINE (the FILE phrase, §2002:14.8.47.1) with no ADVANCING: line 2 fits; only an AT EOP
*>     phrase is written, so nothing is displayed. LINAGE-COUNTER 2, I-O status 00. RETRY FOR 1 SECONDS follows it.
*>   Each WRITE is unshared (OPEN OUTPUT, one connector), so no retry and no lock contention arises.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5620A.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT LPF ASSIGN TO "pb5620a.prt" ORGANIZATION SEQUENTIAL
        LOCK MODE IS MANUAL
        FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD LPF LINAGE IS 5 LINES.
01 P-REC PIC X(4).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 LC PIC 99.
01 W-LINE PIC X(4) VALUE "CCCC".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT LPF
    MOVE "AAAA" TO P-REC
    WRITE P-REC AFTER ADVANCING 1 LINE
        AT END-OF-PAGE DISPLAY "1 AT END-OF-PAGE"
        NOT AT END-OF-PAGE DISPLAY "1 NOT AT END-OF-PAGE"
        WITH LOCK
    END-WRITE
    MOVE LINAGE-COUNTER TO LC
    DISPLAY "1 FS=" FS " LC=" LC
    MOVE "BBBB" TO P-REC
    WRITE P-REC AFTER ADVANCING 9 LINES
        NOT AT EOP DISPLAY "2 NOT AT EOP"
        AT EOP DISPLAY "2 AT EOP"
        RETRY 2 TIMES
        WITH NO LOCK
    END-WRITE
    MOVE LINAGE-COUNTER TO LC
    DISPLAY "2 FS=" FS " LC=" LC
    WRITE FILE LPF FROM W-LINE
        AT EOP DISPLAY "3 AT EOP"
        RETRY FOR 1 SECONDS
    END-WRITE
    MOVE LINAGE-COUNTER TO LC
    DISPLAY "3 FS=" FS " LC=" LC
    CLOSE LPF
    STOP RUN.
