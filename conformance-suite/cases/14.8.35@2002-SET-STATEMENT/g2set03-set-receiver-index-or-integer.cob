      *>--- conformance-case
      *> id: g2set03-set-receiver-index-or-integer
      *> rules: [2002-SR-14.8.35.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.35.2 SR1 (format 1) - "Identifier-1 shall
      *>   reference a data item of class index or an integer data
      *>   item." Each receiver below is one of the two, so each SET
      *>   is accepted:
      *> INT - five integer receivers in one SET: display, binary,
      *>   packed-decimal, PIC 9(3) USAGE NATIONAL (category and class
      *>   numeric in 2002, §2002:8.5.2.9 item 1, kb/Work PB5625
      *>   reading B) and the MESSAGE COUNT item of a 2002 input CD
      *>   (an unsigned integer). §2002:14.8.35.3 GR2c sets each "to
      *>   the occurrence number of the table element referenced by
      *>   index-name-2": IX is at element 3, so every one is 003.
      *> IDX - W-IXD USAGE INDEX (class index) receives IX unchanged
      *>   (GR2b); W-IXD2 receives W-IXD unchanged (GR2b); IX set TO
      *>   W-IXD2 (GR2a.2) refers to element 3 again: TE (IX) = "C"
      *>   and SET W-D TO IX gives 003.
      *> The refusals g2set04 (PIC 9V9, not an integer) and g2set05
      *> (PIC X, neither) are the rule's prohibiting half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SET03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  TE PIC X OCCURS 5 INDEXED BY IX.
       01  TB2.
           05  TE2 PIC X OCCURS 5 INDEXED BY IX2.
       01  W-D   PIC 9(3) VALUE 0.
       01  W-B   PIC 9(4) USAGE BINARY VALUE 0.
       01  W-P   PIC S9(3) USAGE PACKED-DECIMAL VALUE 0.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 0.
       01  W-IXD  USAGE INDEX.
       01  W-IXD2 USAGE INDEX.
       01  W-OUT PIC 9(3).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "ABCDE" TO TB
           MOVE 0 TO IN-COUNT
           SET IX TO 3
           SET W-D W-B W-P W-NU IN-COUNT TO IX
           DISPLAY "INT-D=" W-D
           MOVE W-B TO W-OUT
           DISPLAY "INT-B=" W-OUT
           MOVE W-P TO W-OUT
           DISPLAY "INT-P=" W-OUT
           MOVE W-NU TO W-OUT
           DISPLAY "INT-N=" W-OUT
           MOVE IN-COUNT TO W-OUT
           DISPLAY "INT-CD=" W-OUT
           SET W-IXD TO IX
           SET W-IXD2 TO W-IXD
           SET IX TO 1
           SET IX TO W-IXD2
           DISPLAY "IDX-E=" TE (IX)
           MOVE 0 TO W-D
           SET W-D TO IX
           DISPLAY "IDX-D=" W-D
           STOP RUN.
