      *>--- conformance-case
      *> id: g85_bf09_04
      *> rules: [1985-SR-XIII.3.9.3-2, 1985-SR-XIII.3.9.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR2 - in a report group description entry the
      *> data-name comes right after the level-number and the other
      *> clauses may be written in any sequence.
      *> §1985:XIII.3.9.3 SR7 - in a format 2 entry data-name-1 is
      *> optional and, when present, may only qualify a sum counter
      *> reference.
      *>   cite.py: OK  §1985:XIII.3.9.3 SR2 "the clauses may be written
      *>     in any sequence"
      *>   cite.py: OK  §1985:XIII.3.9.3 SR7 "If present it may be used
      *>     only to qualify a sum counter reference"
      *>   cite.py: OK  §1985:XIII.3.19.4 GR5 "the data-name is the name
      *>     of the sum counter"
      *> DERIVATION. DE-1 writes LINE before TYPE; its items write
      *> SOURCE or VALUE before PICTURE and COLUMN: lines "3 X" and "4
      *> X" (SR2 pins these). CF-F has two format 2 entries named LN1
      *> and LN2 and one unnamed format 2 entry (SR7: optional). Each
      *> named entry holds a sum counter T; T IN LN1 and T IN LN2 are
      *> told apart only by the format 2 names (SR7 pins lines 1-2). T
      *> IN LN1 sums WS-K: 3 + 4 = 7. T IN LN2 writes SUM before PICTURE
      *> and COLUMN and sums WS-K and WS-J (10) on each GENERATE
      *> (§1985:XIII.3.19.4 GR2): 13 + 14 = 27. The USE BEFORE REPORTING
      *> procedure runs before CF-F is presented (§1985:XIII.3.20.4 GR10
      *> c): LN1 T=007, LN2 T=027. The printed lines are 07, ZZ (the
      *> unnamed line) and 027. Fails to translate if an order or the
      *> unnamed format 2 entry is refused, or with equal values if the
      *> qualification resolves to one counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0904.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0904.txt".
           SELECT CHK ASSIGN TO "g85bf0904.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9     VALUE 0.
       01  WS-J    PIC 99    VALUE 10.
       01  WS-O    PIC 999.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 999   VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-1 LINE PLUS 1 TYPE IS DETAIL.
           05  COLUMN 1 SOURCE WS-K PIC 9.
           05  VALUE "X" PIC X COLUMN 3.
       01  CF-F TYPE CONTROL FOOTING FINAL.
           05  LN1 LINE PLUS 1.
               10  T COLUMN 1 PIC 99 SUM WS-K.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC XX VALUE "ZZ".
           05  LN2 LINE PLUS 1.
               10  T SUM WS-K WS-J PIC 999 COLUMN 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-SEC SECTION.
           USE BEFORE REPORTING CF-F.
       CF-PARA.
           MOVE T IN LN1 TO WS-O.
           DISPLAY "LN1 T=" WS-O.
           MOVE T IN LN2 TO WS-O.
           DISPLAY "LN2 T=" WS-O.
       END DECLARATIVES.
       MAIN-SEC SECTION.
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
