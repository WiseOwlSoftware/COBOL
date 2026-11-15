      *>--- conformance-case
      *> id: g2-bwz-cd-record
      *> rules: [2002-SR-13.16.8.2-1, 2002-GR-13.16.8.3-1, 2002-GR-13.16.8.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> BLANK WHEN ZERO in a record description entry following a CD
      *> entry: §2002:A.1.1.2 "A record description consists of a set
      *> of data description entries", so 13.16.8 governs these items.
      *> §2002:13.16.8.2 SR1 - the clause "may be specified only for an
      *> elementary item described by its picture character-string as
      *> category numeric-edited or as numeric without the picture
      *> symbol 'S'": U1 (PIC 999) and E1 (PIC ZZ9.99) are both granted
      *> forms, so the program must be accepted (refusal siblings:
      *> g2-bwz-cd-record-signed, g2-bwz-cd-record-alphanumeric).
      *> §2002:13.16.8.3 GR1 - "the content of the data item is set to
      *> all spaces when the item is a receiving operand and the value
      *> being stored is zero": lines 1, 3 (MOVE 0, MOVE ZERO) and 4
      *> (a COMPUTE receiver).  Line 2: non-zero values are edited by
      *> the picture as usual ("007", "  1.50"); without the clause the
      *> zero lines would show "000" and "  0.00".
      *> §2002:13.16.8.3 GR2 - for a picture of category numeric the
      *> clause "defines the item as numeric-edited", which is class
      *> alphanumeric, so the alphanumeric literal of 88 U1-BLANK is
      *> legal (§2002:13.16.61.2 SR4); were U1 numeric, SR2 would
      *> demand numeric literals and refuse the entry.  The 88 is true
      *> when U1 holds spaces (line 5) and false when it holds "007".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BWZ01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 U1 PIC 999 BLANK WHEN ZERO.
              88 U1-BLANK VALUE "   ".
           05 E1 PIC ZZ9.99 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       P-PARA.
           MOVE 0 TO U1
           MOVE 0 TO E1
           DISPLAY "U1=[" U1 "] E1=[" E1 "]"
           MOVE 7 TO U1
           MOVE 1.5 TO E1
           DISPLAY "U1=[" U1 "] E1=[" E1 "]"
           IF U1-BLANK
               DISPLAY "U1-BLANK WRONG"
           END-IF
           MOVE ZERO TO U1 E1
           DISPLAY "U1=[" U1 "] E1=[" E1 "]"
           MOVE 7 TO U1
           COMPUTE U1 = 7 - 7
           COMPUTE E1 = 1.5 - 1.5
           DISPLAY "U1=[" U1 "] E1=[" E1 "]"
           IF U1-BLANK
               DISPLAY "U1-BLANK TRUE"
           ELSE
               DISPLAY "U1-BLANK FALSE"
           END-IF
           STOP RUN.
