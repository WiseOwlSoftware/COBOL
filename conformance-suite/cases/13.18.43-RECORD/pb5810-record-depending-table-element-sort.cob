*>--- conformance-case
*> id: pb5810-record-depending-table-element-sort
*> rules: [SR-13.18.43.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5810 - as pb5810-record-depending-table-element-indexed, for a sort-merge file description: its RECORD clause is
*> the sequential one (§1985:XI.3.2.4 GR2), and TL, subject to an OCCURS clause, is a table element named without a subscript
*> (§2023:8.4.2.3.3 SR5: "Each table element reference shall be subscripted").
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5810SD.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SF ASSIGN TO "pb5810sd.tmp".
DATA DIVISION.
FILE SECTION.
SD SF RECORD IS VARYING IN SIZE FROM 2 TO 8 CHARACTERS DEPENDING ON TL.
01 SF-R.
   05 SF-K PIC X(2).
   05 SF-D PIC X(6).
WORKING-STORAGE SECTION.
01 G.
   05 TL PIC 99 OCCURS 2.
PROCEDURE DIVISION.
M SECTION.
P.
    SORT SF ON ASCENDING KEY SF-K INPUT PROCEDURE IS IP OUTPUT PROCEDURE IS OP.
    STOP RUN.
IP SECTION.
IP1.
    MOVE "ABCDEFGH" TO SF-R
    MOVE 3 TO TL (1)
    RELEASE SF-R.
OP SECTION.
OP1.
    RETURN SF AT END DISPLAY "AE".
