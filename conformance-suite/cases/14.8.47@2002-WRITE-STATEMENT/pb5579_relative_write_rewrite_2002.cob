*>--- conformance-case
*> id: pb5579_relative_write_rewrite_2002
*> rules: [2002-GR-14.8.47.3-4, 2002-GR-14.8.47.3-7, 2002-GR-14.8.33.3-19]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - plain WRITE and REWRITE rules of a RELATIVE file at COBOL-2002, each read from the 2002 text
*> (cite.py --check, each OK):
*>   §2002:14.8.47.3 GR4 "The figurative constant SPACE when specified in the WRITE statement references one
*>     alphanumeric space character": WRITE record-name-1 FROM SPACE is the MOVE of that one character to the record
*>     (§2002:14.8.47.3 GR3), which repeats it over the whole record, so the record that held XXXXXX reads back as six
*>     spaces (S1, between the brackets).
*>   §2002:14.8.33.3 GR19 "The number of bytes in the runtime representation of literal-1, the data item referenced by
*>     identifier-1, or the record referenced by record-name-1 after any changes made to the record length by the FORMAT
*>     clause shall not be larger than the largest or smaller than the smallest number of bytes allowed by the RECORD IS
*>     VARYING clause": with RECORD VARYING FROM 2 TO 6, a REWRITE of 1 byte (V1) and of 7 bytes (V2) is '44' and leaves
*>     the record as it was (V3 reads 4 bytes back), and one of 3 bytes is '00' (V4, V5).
*>   §2002:14.8.47.3 GR7 "If the locking mode of the write file connector is single record locking, any record lock
*>     associated with that file connector is released by the execution of the WRITE statement": connector A (LOCK MODE
*>     AUTOMATIC, single record locking) reads record 1 and so locks it; connector B's READ of record 1 is then a record
*>     operation conflict (L1, '51', §2002:9.1.12.7); after A's WRITE of record 5 the lock is released and B reads record 1
*>     (L2, '00').
*> Each leg can fail: a record holding X where the SPACE MOVE belongs, a status other than the one the rule names, or a
*> lock still held after the WRITE.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579RW.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RL ASSIGN TO "pb5579rl.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS RK
        FILE STATUS IS FS-RL.
    SELECT LA ASSIGN TO "pb5579lk.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM
        RELATIVE KEY IS KA
        LOCK MODE IS AUTOMATIC
        SHARING WITH ALL OTHER
        FILE STATUS IS FS-LA.
    SELECT LB ASSIGN TO "pb5579lk.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM
        RELATIVE KEY IS KB
        LOCK MODE IS AUTOMATIC
        SHARING WITH ALL OTHER
        FILE STATUS IS FS-LB.
DATA DIVISION.
FILE SECTION.
FD RL
    RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS DEPENDING ON RL-LEN.
01 RL-REC PIC X(6).
FD LA.
01 LA-REC PIC X(4).
FD LB.
01 LB-REC PIC X(4).
WORKING-STORAGE SECTION.
01 RK PIC 9(4).
01 KA PIC 9(4).
01 KB PIC 9(4).
01 RL-LEN PIC 9(4).
01 FS-RL PIC XX.
01 FS-LA PIC XX.
01 FS-LB PIC XX.
PROCEDURE DIVISION.
MAIN.
*> WRITE GR4.
    OPEN OUTPUT RL
    MOVE 1 TO RK
    MOVE "XXXXXX" TO RL-REC
    MOVE 6 TO RL-LEN
    WRITE RL-REC FROM SPACE
    DISPLAY "W1 " FS-RL
    MOVE 2 TO RK
    MOVE "ABCD" TO RL-REC
    MOVE 4 TO RL-LEN
    WRITE RL-REC
    DISPLAY "W2 " FS-RL
    CLOSE RL
    OPEN I-O RL
    MOVE 1 TO RK
    MOVE ALL "Z" TO RL-REC
    READ RL
    DISPLAY "S1 [" RL-REC (1:RL-LEN) "] " RL-LEN " " FS-RL
*> REWRITE GR19.
    MOVE 2 TO RK
    READ RL
    MOVE 1 TO RL-LEN
    REWRITE RL-REC
        INVALID KEY DISPLAY "V1 INVALID " FS-RL
        NOT INVALID KEY DISPLAY "V1 NOT INVALID " FS-RL
    END-REWRITE
    DISPLAY "V1 " FS-RL
    MOVE 7 TO RL-LEN
    REWRITE RL-REC
        INVALID KEY DISPLAY "V2 INVALID " FS-RL
        NOT INVALID KEY DISPLAY "V2 NOT INVALID " FS-RL
    END-REWRITE
    DISPLAY "V2 " FS-RL
    MOVE SPACES TO RL-REC
    READ RL
    DISPLAY "V3 " RL-REC (1:RL-LEN) " " RL-LEN " " FS-RL
    MOVE "PQR" TO RL-REC
    MOVE 3 TO RL-LEN
    REWRITE RL-REC
    DISPLAY "V4 " FS-RL
    MOVE SPACES TO RL-REC
    READ RL
    DISPLAY "V5 " RL-REC (1:RL-LEN) " " RL-LEN " " FS-RL
    CLOSE RL
*> WRITE GR7.
    OPEN OUTPUT LA
    MOVE 1 TO KA
    MOVE "ONE " TO LA-REC
    WRITE LA-REC
    CLOSE LA
    OPEN I-O LA
    OPEN I-O LB
    MOVE 1 TO KA
    READ LA
    DISPLAY "L0 " LA-REC " " FS-LA
    MOVE 1 TO KB
    READ LB
    DISPLAY "L1 " FS-LB
    MOVE 5 TO KA
    MOVE "FIVE" TO LA-REC
    WRITE LA-REC
    DISPLAY "LW " FS-LA
    MOVE 1 TO KB
    READ LB
    DISPLAY "L2 " LB-REC " " FS-LB
    CLOSE LB
    CLOSE LA
    STOP RUN.
