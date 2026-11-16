      *>--- conformance-case
      *> id: g2sw02_select_when_storage_sections_no_effect
      *> rules: [2002-GR-13.16.49.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.49.3 GR2 - "SELECT WHEN clauses of record
      *>   description entries specified in the working-storage
      *>   section, local-storage section, or the linkage section have
      *>   no effect" (cite.py --check OK). §2002:13.16.49.2 SR1 lets
      *>   the clause stand at the 01 level of an entry in each of
      *>   those sections (cite.py --check OK).
      *> The storage-section entry can only matter where a description
      *> other than a file-section record is used for conversion: a
      *> CODE-SET file with NO record description entries.
      *> §2002:13.16.13.3 GR4: "the record description used for
      *>   conversion is the description of the identifier or literal
      *>   specified in the FROM phrase of a WRITE or REWRITE statement
      *>   specifying the FILE option", and "Alphabet-name-1 is used for
      *>   code-set conversion of each data item described with usage
      *>   display in the selected record description entry"
      *>   (cite.py --check OK). §2002:13.3.4.2 SR3b/SR3c require the
      *>   FILE ... FROM and READ ... INTO forms (cite.py --check OK).
      *> WS-REC (working-storage) and LS-REC (local-storage) each carry a
      *> SELECT WHEN whose condition is FALSE (tag "B", condition-name
      *> "A"). By GR2 that clause has no effect, so it neither selects
      *> nor fails anything:
      *>  W  WRITE FILE F FROM WS-REC: WS-REC's description is used for
      *>     conversion and the write succeeds               => "W 00"
      *>     (a clause given file-section effect would select nothing
      *>     and fail the write with status 45, §2002:13.16.49.3 GR4)
      *>  MEDIUM  read back raw through the plain FD FR: WS-REC's
      *>     display items B X Y Z are converted to EBCDIC (X'C2' X'E7'
      *>     X'E8' X'E9' = 194 231 232 233, §2002:13.16.13.3 GR6b)
      *>  R  READ F INTO LS-REC: the implied alphanumeric record
      *>     (§2002:14.8.29.3 GR5) is converted back (GR6a) and moved;
      *>     LS-REC's false clause has no effect      => "R 00 BXYZ"
      *>  K  the linkage record LK-REC of a called program addresses
      *>     WS-REC (BY REFERENCE)                         => "K BXYZ"
      *>  M  MOVE "Q" TO LK-KEY changes the tag; nothing is selected or
      *>     re-described                                  => "M QXYZ"
      *>  A  the caller sees the change through WS-REC     => "A QXYZ"
      *> Why it can fail: a storage-section clause refused, or evaluated
      *> as a file-section clause (W or R with status 45), or the record
      *> not written from WS-REC's description (MEDIUM bytes).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SW02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2sw02.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FR ASSIGN TO "g2sw02.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F CODE-SET IS A-EBC RECORD CONTAINS 4 CHARACTERS.
       FD  FR.
       01  FR-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  I PIC 9.
       01  N1 PIC 999.
       01  WS-REC SELECT WHEN WS-IS-A.
           05 WS-KEY PIC X VALUE "B".
              88 WS-IS-A VALUE "A".
           05 WS-TXT PIC X(3) VALUE "XYZ".
       LOCAL-STORAGE SECTION.
       01  LS-REC SELECT WHEN LS-IS-A.
           05 LS-KEY PIC X.
              88 LS-IS-A VALUE "A".
           05 LS-TXT PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F
           WRITE FILE F FROM WS-REC
           DISPLAY "W " FS
           CLOSE F
           OPEN INPUT FR
           READ FR END-READ
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 4
               COMPUTE N1 = FUNCTION ORD(FR-REC(I:1)) - 1
               DISPLAY "MEDIUM " I " " N1
           END-PERFORM
           CLOSE FR
           OPEN INPUT F
           READ F INTO LS-REC END-READ
           DISPLAY "R " FS " " LS-REC
           CLOSE F
           CALL "G2SW02S" USING WS-REC
           DISPLAY "A " WS-REC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SW02S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-REC SELECT WHEN LK-IS-A.
           05 LK-KEY PIC X.
              88 LK-IS-A VALUE "A".
           05 LK-TXT PIC X(3).
       PROCEDURE DIVISION USING LK-REC.
       SUB-P.
           DISPLAY "K " LK-REC
           MOVE "Q" TO LK-KEY
           DISPLAY "M " LK-REC
           GOBACK.
       END PROGRAM G2SW02S.
       END PROGRAM G2SW02.
