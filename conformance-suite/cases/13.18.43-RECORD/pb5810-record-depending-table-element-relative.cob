*>--- conformance-case
*> id: pb5810-record-depending-table-element-relative
*> rules: [SR-13.18.43.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5810 - as pb5810-record-depending-table-element-indexed, for a relative file: its RECORD clause is the sequential
*> one (§1985:VIII.3.2.4 GR5), and TL, subject to an OCCURS clause, is a table element named without a subscript
*> (§2023:8.4.2.3.3 SR5: "Each table element reference shall be subscripted").
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5810RL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RL ASSIGN TO "pb5810rl.rel"
        ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD RL RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS DEPENDING ON TL.
01 RL-R PIC X(8).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 G.
   05 TL PIC 99 OCCURS 2.
PROCEDURE DIVISION.
P.
    OPEN OUTPUT RL
    MOVE 9 TO TL (1)
    WRITE RL-R INVALID KEY DISPLAY "IK" END-WRITE
    STOP RUN.
