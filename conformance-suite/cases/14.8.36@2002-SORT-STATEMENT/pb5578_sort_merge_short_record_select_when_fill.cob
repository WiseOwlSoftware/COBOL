*>--- conformance-case
*> id: pb5578_sort_merge_short_record_select_when_fill
*> rules: [2002-GR-14.8.36.3-6, 2002-GR-14.8.36.3-15, 2002-GR-14.8.23.3-2, 2002-GR-14.8.23.3-13]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5578 - arm b) of the short-record fill of a SORT or MERGE transfer at COBOL-2002: a file with several record
*> description entries whose SELECT WHEN clauses choose the description, so the fill is chosen record by record.
*> THE RULE (cite.py --check, each OK):
*>   §2002:14.8.36.3 GR6b "If there are multiple record description entries associated with the file referenced by
*>     file-name-2 and the descriptions include a SELECT WHEN clause, the rules of the SELECT WHEN clause are applied to the
*>     record to select its description. When the record is described as a national data item or as an elementary data
*>     item of usage national and of category numeric, numeric-edited, or boolean, the record is filled with national
*>     space characters." GR15b is the same sentence for the GIVING transfer, and §2002:14.8.23.3 GR2b and GR13b are
*>     MERGE's.
*>   §2002:13.16.49.3 GR1 "Any SELECT WHEN clauses of record description entries specified in the file section are
*>     evaluated for READ, REWRITE, and WRITE statements associated with that file when a CODE-SET clause or FORMAT clause
*>     is specified for the file": so the files that select carry CODE-SET IS AL (ALPHABET AL IS NATIVE, which leaves every
*>     byte as it is); a file without either clause has SELECT WHEN clauses no rule evaluates and takes arm c)
*>     (docs/conformance/3/D-SMF.md, reading a).
*> THE FILES. FU (CODE-SET, RECORD VARYING 2 TO 4) holds the 2-byte national record FU-N, N"N" (bytes 000 078), selected by
*> IS-NAT, and the 4-byte group FU-A, N"A" "ab" (000 065 097 098), selected by OTHER. FU2 is a plain file holding the one
*> 4-byte record 000 066 099 100. SF is a fixed 6-byte SD (the USING transfers fill), SV a VARYING 2 TO 6 SD whose records
*> stay short (the GIVING transfers fill). FA is a plain fixed 6-byte file; FG (CODE-SET) has the national entry FG-N
*> (IS-NAT) and the 6-byte group FG-A (OTHER), so its records are 6 bytes (the implicit RECORD clause, DOC-A.1-147).
*> Each line shows the six bytes of a record read back. A national space is 000 032 (UTF-16BE), an alphanumeric one 032.
*>   SU  SORT SF USING FU GIVING FA (GR6b): the alphanumeric record takes 032 032, the national one 000 032 000 032.
*>   SG  SORT SV USING FU GIVING FG (GR15b): the same two fills, chosen by FG's own SELECT WHEN clauses.
*>   MU  MERGE SF USING FU FU2 GIVING FA (MERGE GR2b): FU's two records as SU; FU2's one record (no SELECT WHEN, one
*>       alphanumeric description, arm c) takes 032 032.
*>   MG  MERGE SV USING FU FU2 GIVING FG (MERGE GR13b): as MU, the fill chosen by FG's clauses.
*> A leg fails if a national record takes the alphanumeric space (the selection not applied) or an alphanumeric record the
*> national space (the selection applied to the wrong entry).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5578SW.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET AL IS NATIVE.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FU ASSIGN TO "pb5578fu.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
    SELECT FU2 ASSIGN TO "pb5578f2.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
    SELECT SF ASSIGN TO "pb5578sf.tmp".
    SELECT SV ASSIGN TO "pb5578sv.tmp".
    SELECT FA ASSIGN TO "pb5578fa.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
    SELECT FG ASSIGN TO "pb5578fg.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
DATA DIVISION.
FILE SECTION.
FD FU CODE-SET IS AL
    RECORD IS VARYING IN SIZE FROM 2 TO 4 CHARACTERS DEPENDING ON LN.
01 FU-N PIC N SELECT WHEN IS-NAT.
   88 IS-NAT VALUE N"N".
01 FU-A SELECT WHEN OTHER.
   05 FU-A-K PIC N.
   05 FU-A-D PIC XX.
FD FU2
    RECORD IS VARYING IN SIZE FROM 2 TO 4 CHARACTERS DEPENDING ON LN.
01 FU2-REC PIC X(4).
SD SF.
01 SF-REC.
   05 SF-K PIC XX.
   05 SF-D PIC X(4).
SD SV
    RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
01 SV-REC.
   05 SV-K PIC XX.
   05 SV-D PIC X(4).
FD FA.
01 FA-REC PIC X(6).
FD FG CODE-SET IS AL.
01 FG-N PIC N SELECT WHEN IS-FG-NAT.
   88 IS-FG-NAT VALUE N"N".
01 FG-A SELECT WHEN OTHER.
   05 FG-A-K PIC N.
   05 FG-A-D PIC X(4).
WORKING-STORAGE SECTION.
01 LN PIC 9(4).
01 FS PIC XX.
01 I PIC 9.
01 B PIC 999.
01 TAG PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT FU
    MOVE N"A" TO FU-A-K  MOVE "ab" TO FU-A-D  MOVE 4 TO LN  WRITE FU-A
    MOVE N"N" TO FU-N  MOVE 2 TO LN  WRITE FU-N
    DISPLAY "FU " FS
    CLOSE FU
    OPEN OUTPUT FU2
    MOVE X"00426364" TO FU2-REC  MOVE 4 TO LN  WRITE FU2-REC
    DISPLAY "FU2 " FS
    CLOSE FU2
    SORT SF ON ASCENDING KEY SF-K USING FU GIVING FA
    MOVE "SU" TO TAG  PERFORM SHOW-FA
    SORT SV ON ASCENDING KEY SV-K USING FU GIVING FG
    MOVE "SG" TO TAG  PERFORM SHOW-FG
    MERGE SF ON ASCENDING KEY SF-K USING FU FU2 GIVING FA
    MOVE "MU" TO TAG  PERFORM SHOW-FA
    MERGE SV ON ASCENDING KEY SV-K USING FU FU2 GIVING FG
    MOVE "MG" TO TAG  PERFORM SHOW-FG
    STOP RUN.
SHOW-FA.
    OPEN INPUT FA
    READ FA
    PERFORM UNTIL FS NOT = "00"
        DISPLAY TAG WITH NO ADVANCING
        PERFORM VARYING I FROM 1 BY 1 UNTIL I > 6
            COMPUTE B = FUNCTION ORD (FA-REC (I:1)) - 1
            DISPLAY " " B WITH NO ADVANCING
        END-PERFORM
        DISPLAY " "
        READ FA
    END-PERFORM
    CLOSE FA.
SHOW-FG.
    OPEN INPUT FG
    READ FG
    PERFORM UNTIL FS NOT = "00"
        DISPLAY TAG WITH NO ADVANCING
        PERFORM VARYING I FROM 1 BY 1 UNTIL I > 6
            COMPUTE B = FUNCTION ORD (FG-A (I:1)) - 1
            DISPLAY " " B WITH NO ADVANCING
        END-PERFORM
        DISPLAY " "
        READ FG
    END-PERFORM
    CLOSE FG.
