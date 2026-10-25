*>--- conformance-case
*> id: pb493_report_group_sign_inherited
*> rules: [GR-13.18.52.4-1, GR-13.18.52.4-2, GR-13.18.52.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB493 - a SIGN clause written on a REPORT GROUP description entry that is a group applies to each
      *> signed numeric printable item subordinate to it, exactly as a data division group's clause does.
      *> GR1: "The SIGN clause specifies the position and the mode of representation of the operational sign for the
      *>   numeric item to which it applies, or for each numeric item subordinate to the group to which it applies."
      *>   cite.py: OK  13.18.52.4 1)  (General rules)
      *> GR2: "... the SIGN clause specified in the subordinate group item takes precedence for that subordinate group
      *>   item."   cite.py: OK  13.18.52.4 2)  (General rules)
      *> GR3: "... the SIGN clause specified in that elementary entry takes precedence for that elementary entry."
      *>   cite.py: OK  13.18.52.4 3)  (General rules)
      *> GR6 b): "The operational signs for positive and negative are the basic special characters '+' and '-',
      *>   respectively."   cite.py: OK  13.18.52.4 6) b)  (General rules)
      *> SR1's third bullet admits the clause on "an alphanumeric group item, national group item, or strongly-typed
      *>   group item" (cite.py: OK  13.18.52.3 1)), and a report group entry with subordinate entries is one; 13.15.3
      *>   does not restrict the clause there.
      *> Every sign below is SEPARATE, so GR6 a) and b) fix the whole image and no implementor determination enters:
      *> WN = -5, WP = +7.
      *>   DA (SIGN LEADING SEPARATE on the level 1 entry, GR1):  col 1 "-005", col 6 "+007".
      *>   DB (SIGN TRAILING SEPARATE on level 1) / first LINE (SIGN LEADING SEPARATE, GR2 wins for that line):
      *>     col 1 "-005" (the line's clause), col 6 "005-" (its own TRAILING clause, GR3).
      *>   DB / second LINE (no clause, the level 1 clause reaches it): col 1 "005-"; col 6 PIC 9(3) has no 'S' and
      *>     GR1 says "The SIGN clause applies only to numeric items whose picture character-string contains the
      *>     symbol 'S'", so it prints "007".
      *> Before the fix the report walk handed the enclosing USAGE down and dropped the SIGN: DA printed "00N" and
      *> "00G" (the implementor's trailing over-punch default, GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB493RG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB493RG.TXT".
           SELECT CHK ASSIGN TO "PB493RG.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WN      PIC S9(3) VALUE -5.
       01  WP      PIC S9(3) VALUE +7.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1 SIGN LEADING SEPARATE.
           02  COLUMN 1 PIC S9(3) SOURCE WN.
           02  COLUMN 6 PIC S9(3) SOURCE WP.
       01  DB TYPE DE SIGN TRAILING SEPARATE.
           02  LINE PLUS 1 SIGN LEADING SEPARATE.
               03  COLUMN 1 PIC S9(3) SOURCE WN.
               03  COLUMN 6 PIC S9(3) SOURCE WN SIGN TRAILING SEPARATE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC S9(3) SOURCE WN.
               03  COLUMN 6 PIC 9(3) SOURCE WP.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           GENERATE DB.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
