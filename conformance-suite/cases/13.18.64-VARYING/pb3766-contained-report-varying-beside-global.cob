      *>--- conformance-case
      *> id: pb3766-contained-report-varying-beside-global
      *> rules: [SR-13.18.64.3-2, GR-13.18.27.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766, review finding R2-1. A contained program's report VARYING counter is not "defined
      *> elsewhere in the source element" because its container defines a GLOBAL data-name of the same spelling.
      *>
      *> THE RULE. ISO/IEC 1989:2023 13.18.64.3 SR2: "Data-name-1 shall not be defined elsewhere in the source
      *> element, except as data-name-1 in another VARYING clause of an entry not subordinate to the subject of
      *> the current entry." The container PB3766RO defines the GLOBAL data item K; the contained program
      *> PB3766RI is a different source element, whose entry DET-A names K in its VARYING clause. 8.4.6.2.1 3) a)
      *> makes the declaration in the contained program the referenced one: "If the name is declared in source
      *> element B, the item in source element B is the referenced item." The compiler inherited the container's
      *> GLOBAL names before the VARYING screen ran and refused the contained program with COBOL1559.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. VARYING K FROM 2 BY 3 gives the three occurrences of the item at
      *> columns 1, 3 and 5 the values 2, 2 + 3 = 5 and 5 + 3 = 8 (13.18.64.4 3) a) and b)), printed as
      *> "2 5 8". The container's K is a different data item: it keeps its VALUE 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766RO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 GLOBAL VALUE 7.
       PROCEDURE DIVISION.
           CALL "PB3766RI"
           DISPLAY "OUTER K=" K
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766RI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3766r.txt".
           SELECT CHK ASSIGN TO "pb3766r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-V.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-V.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING K FROM 2 BY 3
                   SOURCE K.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRT.
           INITIATE R-V.
           GENERATE DET-A.
           TERMINATE R-V.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           DISPLAY "LINE [" WS-LINE(1:5) "]".
           EXIT PROGRAM.
       TAKE-BYTE.
           IF CHK-REC NOT = X"0A" AND CHK-REC NOT = X"0D"
                   AND CHK-REC NOT = X"0C"
               ADD 1 TO WS-I
               MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-IF.
       END PROGRAM PB3766RI.
       END PROGRAM PB3766RO.
