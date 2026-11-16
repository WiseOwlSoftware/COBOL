      *>--- conformance-case
      *> id: g2sum02_sum_counter_digits_sign
      *> rules: [2002-GR-13.16.52.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.3 GR1 - each SUM entry has an independent sum
      *> counter and size error indicator; the counter's integral and
      *> fractional digits come from the entry's PICTURE "excluding
      *> insertion editing characters"; the counter "is signed, whether
      *> or not the corresponding PICTURE clause has an operational sign".
      *>
      *> DERIVATION (2002 text only).  Two GENERATE DET: WS-P = -3 then
      *> +5; WS-Q = 5, WS-R = 50, WS-F = 1.25 both times.  GR3: each
      *> addition follows ADD with ON SIZE ERROR (a size error leaves the
      *> counter unchanged and sets its indicator); GR4: a set indicator
      *> fills the printable item with spaces, otherwise the counter is
      *> MOVEd to it.
      *>  KA PIC 99 SUM WS-P: signed counter -3 + 5 = 2           => 02
      *>     (an unsigned counter would hold 3, then 8: 08)
      *>  KB, KC PIC 99 SUM WS-Q: two independent counters, 10 each
      *>                                                     => 10 10
      *>  KE PIC 9 SUM WS-Q: one digit; 5 + 5 = 10 is a size error, the
      *>     indicator of KE alone is set                    => (space)
      *>  KG PIC 99 SUM WS-Q: same addend, own indicator unset  => 10
      *>  KH PIC 9,9 SUM WS-R: two digits (the comma is insertion
      *>     editing); 50 + 50 = 100 is a size error         => 3 spaces
      *>  KI PIC 9,99 SUM WS-R: three digits                   => 1,00
      *>  KJ PIC 9.9 SUM WS-F: one fractional digit; each ADD truncates
      *>     (14.6.3): 1.25 -> 1.2, 1.2 + 1.25 = 2.45 -> 2.4     => 2.4
      *>     (a two-fraction-digit counter would hold 2.50: 2.5)
      *> cite.py --check: OK §2002:13.16.52.3 GR1, GR3, GR4; §2002:14.6.3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum02.txt".
           SELECT CHK ASSIGN TO "g2sum02.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-P    PIC S9    VALUE 0.
       01  WS-Q    PIC 9     VALUE 5.
       01  WS-R    PIC 99    VALUE 50.
       01  WS-F    PIC 9V99  VALUE 1.25.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  KA COLUMN 1  PIC 99   SUM WS-P.
           05  KB COLUMN 4  PIC 99   SUM WS-Q.
           05  KC COLUMN 7  PIC 99   SUM WS-Q.
           05  KE COLUMN 10 PIC 9    SUM WS-Q.
           05  KG COLUMN 12 PIC 99   SUM WS-Q.
           05  KH COLUMN 15 PIC 9,9  SUM WS-R.
           05  KI COLUMN 19 PIC 9,99 SUM WS-R.
           05  KJ COLUMN 24 PIC 9.9  SUM WS-F.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE -3 TO WS-P.
           GENERATE DET.
           MOVE 5 TO WS-P.
           GENERATE DET.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
