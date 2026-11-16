      *>--- conformance-case
      *> id: g2-value-cd-condition-name
      *> rules: [2002-GR-13.16.61.3-16]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.3 GR16 - "The characteristics of a
      *> condition-name are implicitly those of its conditional
      *> variable."  The conditional variables are items of a record
      *> description entry following a CD entry (§2002:A.1.1.2).
      *> S2 is signed numeric (stored "012-" with its group's
      *> SIGN TRAILING SEPARATE): S2-NEG (-999 THRU -1) is a numeric
      *> test, true for -12 and false for +12, and SET S2-NEG TO TRUE
      *> stores -999 as a signed numeric value, "999-".  An alphanumeric
      *> reading of "012-" would not fall in the range.
      *> B1 is numeric-edited (PIC 999 BLANK WHEN ZERO,
      *> §2002:13.16.8.3 GR2), class alphanumeric: B1-ZERO ("   ") is
      *> true after MOVE 0 (the item holds spaces, §2002:13.16.8.3 GR1)
      *> and B1-SEVEN ("007") after MOVE 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL06.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC SIGN IS TRAILING SEPARATE.
           05 S2 PIC S9(3).
              88 S2-NEG VALUES -999 THRU -1.
           05 B1 PIC 9(3) BLANK WHEN ZERO.
              88 B1-ZERO VALUE "   ".
              88 B1-SEVEN VALUE "007".
       PROCEDURE DIVISION.
       P-PARA.
           MOVE -12 TO S2
           IF S2-NEG DISPLAY "S2-NEG TRUE" ELSE DISPLAY "S2-NEG FALSE"
           END-IF
           MOVE 12 TO S2
           IF S2-NEG DISPLAY "S2-NEG TRUE" ELSE DISPLAY "S2-NEG FALSE"
           END-IF
           SET S2-NEG TO TRUE
           DISPLAY "S2=[" CDREC(1:4) "]"
           MOVE 0 TO B1
           IF B1-ZERO DISPLAY "B1-ZERO TRUE" ELSE
               DISPLAY "B1-ZERO FALSE" END-IF
           IF B1-SEVEN DISPLAY "B1-SEVEN TRUE" ELSE
               DISPLAY "B1-SEVEN FALSE" END-IF
           MOVE 7 TO B1
           IF B1-ZERO DISPLAY "B1-ZERO TRUE" ELSE
               DISPLAY "B1-ZERO FALSE" END-IF
           IF B1-SEVEN DISPLAY "B1-SEVEN TRUE" ELSE
               DISPLAY "B1-SEVEN FALSE" END-IF
           STOP RUN.
