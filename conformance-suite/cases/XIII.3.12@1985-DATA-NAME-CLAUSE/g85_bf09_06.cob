      *>--- conformance-case
      *> id: g85_bf09_06
      *> rules: [1985-SR-XIII.3.12.3-1, 1985-GR-XIII.3.12.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.12.3 SR1 - in the Report Section data-name-1 need
      *> not appear: the line entries and most printable entries here
      *> have none.
      *> §1985:XIII.3.12.4 GR1 - data-name-1 is given where it is
      *> needed: a. DE-A, DE-B for GENERATE and CF-1 for USE; b. TA
      *> referenced by a SOURCE clause and in the Procedure Division; c.
      *> DE-A and DE-B in UPON phrases; d. L2 and L3 qualifying the two
      *> sum counters TB.
      *>   cite.py: OK  §1985:XIII.3.12.3 SR1 "data-name-1 need not
      *>     appear in a data description entry"
      *>   cite.py: OK  §1985:XIII.3.12.4 GR1 "When a detail report
      *>     group is referenced in the UPON phrase of the SUM clause"
      *>   cite.py: OK  §1985:XIII.3.18.3 SR1 "Sum counter that is part
      *>     of the report within which the SOURCE clause appears"
      *>   cite.py: OK  §1985:XIII.3.19.4 GR8 "The UPON phrase provides
      *>     the capability to accomplish selective subtotalling"
      *> DERIVATION. GENERATEs: DE-A with WS-K 2, DE-B with 5, DE-A with
      *> 3. TA (UPON DE-A) = 2 + 3 = 5; TB IN L2 (no UPON, every
      *> GENERATE) = 10; TB IN L3 (UPON DE-B) = 5. MAIN TA=005 is read
      *> before TERMINATE; the USE BEFORE REPORTING CF-1 procedure runs
      *> before CF-1 is presented (§1985:XIII.3.20.4 GR10c. Printed:
      *> 2, "B 5", 3, "05 05" (TA and SOURCE TA), 10, 05. Fails to
      *> translate if an unnamed entry is refused or a named reference
      *> does not resolve; fails with other sums if an UPON name or a
      *> qualifier picks the wrong group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0906.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0906.txt".
           SELECT CHK ASSIGN TO "g85bf0906.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9     VALUE 0.
       01  WS-O    PIC 999.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 999   VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-A TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-K.
       01  DE-B TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC X VALUE "B".
           05  COLUMN 3 PIC 9 SOURCE WS-K.
       01  CF-1 TYPE CF FINAL.
           05  LINE PLUS 1.
               10  TA COLUMN 1 PIC 99 SUM WS-K UPON DE-A.
               10  COLUMN 4 PIC 99 SOURCE TA.
           05  L2 LINE PLUS 1.
               10  TB COLUMN 1 PIC 99 SUM WS-K.
           05  L3 LINE PLUS 1.
               10  TB COLUMN 1 PIC 99 SUM WS-K UPON DE-B.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-SEC SECTION.
           USE BEFORE REPORTING CF-1.
       CF-PARA.
           MOVE TA TO WS-O.
           DISPLAY "TA=" WS-O.
           MOVE TB IN L2 TO WS-O.
           DISPLAY "L2 TB=" WS-O.
           MOVE TB IN L3 TO WS-O.
           DISPLAY "L3 TB=" WS-O.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE 2 TO WS-K.
           GENERATE DE-A.
           MOVE 5 TO WS-K.
           GENERATE DE-B.
           MOVE 3 TO WS-K.
           GENERATE DE-A.
           MOVE TA TO WS-O.
           DISPLAY "MAIN TA=" WS-O.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   IF WS-I NOT > 20
                       MOVE CHK-REC TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
