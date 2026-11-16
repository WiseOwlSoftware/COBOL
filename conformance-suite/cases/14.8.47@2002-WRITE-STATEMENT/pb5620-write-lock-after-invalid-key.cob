*>--- conformance-case
*> id: pb5620-write-lock-after-invalid-key
*> rules: [2002-SR-14.8.47.2-3, SR-14.9.51.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5620 - §2002:14.8.47.2 SR3 "If the organization of the write file is indexed or relative, format 2 shall be
*> specified." (§2023:14.9.51.3 SR3), and every edition's Format 2 prints [ retry-phrase ] and WITH LOCK / WITH NO LOCK
*> BEFORE the INVALID KEY phrases (§2002:14.8.47.1, §2023:14.9.51.2); only the 2002 Format 1 prints the pair after a
*> conditional phrase, and that phrase is END-OF-PAGE. §2023:5.2.1: "The words, phrases, clauses, punctuation, and
*> operands in each general format shall be written in the compilation group in the sequence given in the general
*> format, unless otherwise specified by the rules of that format." Refused at every edition that has the phrases.
*> Expected: COBOL2987.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5620N3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb5620n3.dat" ORGANIZATION INDEXED
        ACCESS MODE RANDOM RECORD KEY IS IX-KEY
        LOCK MODE IS MANUAL.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT IXF
    MOVE "AAAA" TO IX-KEY
    WRITE IX-REC
        INVALID KEY DISPLAY "INVALID"
        WITH LOCK
    END-WRITE
    CLOSE IXF
    STOP RUN.
