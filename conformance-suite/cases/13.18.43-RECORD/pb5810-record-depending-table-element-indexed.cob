*>--- conformance-case
*> id: pb5810-record-depending-table-element-indexed
*> rules: [SR-13.18.43.3-6, FMT-13.18.43.2, FMT-13.4.6.2, SR-13.4.5.3-5, SR-13.4.5.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5810 - §2023:13.18.43.2 Format 2 prints "DEPENDING ON data-name-1", a bare data-name, and
*> §2023:8.4.2.3.3 SR5 requires "Each table element reference shall be subscripted" outside seven contexts, none of which is
*> the RECORD clause. TL is an item subject to an OCCURS clause, so naming it here without a subscript is a table element
*> reference with no subscript. The clause is the same for an indexed file (§1985:IX.3.2.4 GR5). The program used to
*> compile at every edition and its WRITE ignored the length register (status 00 where an elementary item gives 44).
*> The accepted sibling is pb5810_record_depending_elementary_relative.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5810IX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT XF ASSIGN TO "pb5810ix.dat"
        ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
        RECORD KEY IS XF-K FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD XF RECORD IS VARYING IN SIZE FROM 4 TO 12 CHARACTERS DEPENDING ON TL.
01 XF-R.
   05 XF-K PIC X(4).
   05 XF-D PIC X(8).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 G.
   05 TL PIC 99 OCCURS 2.
PROCEDURE DIVISION.
P.
    OPEN OUTPUT XF
    MOVE 13 TO TL (1)
    WRITE XF-R INVALID KEY DISPLAY "IK" END-WRITE
    STOP RUN.
