*>--- conformance-case
*> id: pb2728-read-file-unprinted
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.30.2 - BOTH printed READ formats begin `READ file-name-1`; neither
*> prints a FILE word before the file-name, and no earlier edition did either.
*>   cite.py --check 14.9.30.2 "file-name-1 RECORD" -> OK (Format 1 and Format 2)
*>   cite.py --check 5.2.1 "shall be written in the compilation group in the sequence
*>     given in the general format" -> OK
*> The `{ record-name-1 | FILE file-name-1 }` choice is WRITE's (14.9.51.2) and REWRITE's (14.9.35.2) alone (Annex
*> A.4.13, supported: kb/Work PB5579, pb5579_write_file_sequential / pb5579_rewrite_file). readStatement carried a copy
*> of that two-arm head that nothing refused, so this program compiled clean at every --std and RAN as a plain READ
*> (kb/Work PB2728). FILE is a reserved word and never a file-name, so with the arm gone the parser
*> stops at it. The legal spelling is `READ F-IO INTO WS-IN`.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB2728RF.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IN ASSIGN TO "pb2728rf.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IN.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F-IN.
    READ FILE F-IN RECORD
        AT END DISPLAY "EOF"
    END-READ.
    CLOSE F-IN.
    STOP RUN.
