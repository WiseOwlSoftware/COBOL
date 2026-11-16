      *>--- conformance-case
      *> id: g85_bf09_05
      *> rules: [1985-GR-XIII.3.11.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.4 GR1 - an entry with a COLUMN NUMBER clause is
      *> presented on the print line (the object of its SOURCE or VALUE
      *> clause, or its sum counter); an entry without one is not
      *> presented.
      *>   cite.py: OK  §1985:XIII.3.11.4 GR1 "The absence of a COLUMN
      *>     NUMBER clause indicates that the entry is not to be
      *>     presented on a print line"
      *> Table 1 of §1985:XIII.3.9.3 SR11 permits SOURCE and SUM entries
      *> without COLUMN; SR10 e requires COLUMN with VALUE.
      *> DERIVATION. DE-1 presents ABC (column 1) and WS-K (column 5);
      *> the SOURCE WS-HID entry has no COLUMN and is not presented:
      *> "ABC 3", "ABC 4" (column 4 is a space supplied by the RWCS,
      *> GR3). CF-F's TOT sums WS-K (7) without COLUMN and is not
      *> presented; the SOURCE TOT entry at column 5 and the SUM entry
      *> at column 8 are: "TOT=07 07". Fails with HIDE or an extra 07 on
      *> a line if an entry without COLUMN is presented, or with a
      *> missing value if a COLUMN entry is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0905.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0905.txt".
           SELECT CHK ASSIGN TO "g85bf0905.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9     VALUE 0.
       01  WS-HID  PIC X(4)  VALUE "HIDE".
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 999   VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-1 TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "ABC".
               03  PIC X(4) SOURCE WS-HID.
               03  COLUMN 5 PIC 9 SOURCE WS-K.
       01  CF-F TYPE CF FINAL.
           02  LINE PLUS 1.
               03  TOT PIC 99 SUM WS-K.
               03  COLUMN 1 PIC X(4) VALUE "TOT=".
               03  COLUMN 5 PIC 99 SOURCE TOT.
               03  COLUMN 8 PIC 99 SUM WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE 3 TO WS-K.
           GENERATE DE-1.
           MOVE 4 TO WS-K.
           GENERATE DE-1.
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
