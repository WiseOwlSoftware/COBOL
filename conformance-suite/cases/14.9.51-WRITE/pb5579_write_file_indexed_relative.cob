*>--- conformance-case
*> id: pb5579_write_file_indexed_relative
*> rules: [FMT-14.9.51.2, SR-14.9.51.3-7, GR-14.9.51.4-8]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - WRITE FILE file-name-1 FROM ... on an INDEXED and a RELATIVE file, the printed Format 2
*> (random) of §2002:14.8.47.1 and §2023:14.9.51.2, which carries the same `{ record-name-1 | FILE file-name-1 }`
*> choice as Format 1.
*> THE RULES (cite.py --check, each OK):
*>   §2002:14.8.47.2 SR3 "If the organization of the write file is indexed or relative, format 2 shall be
*>     specified." - so the FILE phrase is admitted with the INVALID KEY phrases here.
*>   §2002:14.8.47.3 GR5 "The result of execution of a WRITE statement with the FILE phrase is equivalent to the
*>     execution of the following in the order specified: ... MOVE identifier-1 TO implicit-record-1 ... WRITE
*>     implicit-record-1" - the record released is the FROM operand under its own description (a literal-1:
*>     the class, category, usage and length of the literal).
*>   §2002:14.8.47.3 GR37 (indexed) and GR29 (relative): "The invalid key condition exists under the following
*>     circumstances" - among them a WRITE of a prime record key (an RRN) already present in the file: I-O status 22.
*> EXPECTED: the indexed file's record is the 8 characters "ABC12345" (key = positions 1-3), so a READ of key ABC
*> returns it; the second WRITE of the same key gives status 22 and runs the INVALID KEY imperative; the relative
*> record 3 is written once (00) and the second write of record 3 gives 22 / INVALID KEY. Each leg can fail: a
*> record built from the largest record view instead of the operand would not carry the key "ABC".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579WI.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FI ASSIGN TO "pb5579wi.dat" ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC RECORD KEY IS KEY1
        FILE STATUS IS FS.
    SELECT FR ASSIGN TO "pb5579wr.dat" ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM RELATIVE KEY IS RK
        FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD FI.
01 RI.
   05 KEY1 PIC X(3).
   05 DAT1 PIC X(5).
FD FR.
01 RR PIC X(8).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 RK PIC 9(3).
01 W8 PIC X(8) VALUE "ABC12345".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT FI.
    WRITE FILE FI FROM W8
        INVALID KEY DISPLAY "FI invalid " FS
        NOT INVALID KEY DISPLAY "FI written " FS
    END-WRITE.
    WRITE FILE FI FROM W8
        INVALID KEY DISPLAY "FI invalid " FS
        NOT INVALID KEY DISPLAY "FI written " FS
    END-WRITE.
    CLOSE FI.
    OPEN INPUT FI.
    MOVE "ABC" TO KEY1.
    READ FI.
    DISPLAY "FI read " FS " " RI.
    CLOSE FI.
    OPEN OUTPUT FR.
    MOVE 3 TO RK.
    WRITE FILE FR FROM "REL-REC3"
        INVALID KEY DISPLAY "FR invalid " FS
        NOT INVALID KEY DISPLAY "FR written " FS
    END-WRITE.
    MOVE 3 TO RK.
    WRITE FILE FR FROM W8
        INVALID KEY DISPLAY "FR invalid " FS
        NOT INVALID KEY DISPLAY "FR written " FS
    END-WRITE.
    CLOSE FR.
    OPEN INPUT FR.
    MOVE 3 TO RK.
    READ FR.
    DISPLAY "FR read " FS " " RR.
    CLOSE FR.
    STOP RUN.
