      *>--- conformance-case
      *> id: pb5816_unstring_zero_length_sender_terminates
      *> rules: [2002-GR-14.8.44.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5816 - §2002:14.8.44.3 GR2: "If the data item referenced by identifier-1 is a zero-length item, execution of the
      *> UNSTRING statement terminates immediately" (§2023:14.9.48.4 GR2 the same), ahead of GR15a's overflow test, so legs A and
      *> C run neither ON OVERFLOW nor NOT ON OVERFLOW and leave WX and the pointer untouched. Leg B (WC = 1, pointer 2
      *> past the size 1) is the overflow of GR15a. The COBOL-85 twin of this program is pb5816_unstring_zero_length_sender_overflow.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5816B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC X(3) VALUE "QQQ".
       01 WP PIC 99 VALUE 1.
       01 WC PIC 9 VALUE 0.
       01 OG.
          05 OE PIC X OCCURS 0 TO 8 DEPENDING ON WC.
       PROCEDURE DIVISION.
       P0.
           UNSTRING OG INTO WX WITH POINTER WP
               ON OVERFLOW DISPLAY "A OVERFLOW"
               NOT ON OVERFLOW DISPLAY "A NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "A X=[" WX "] P=" WP.
           UNSTRING OG INTO WX
               ON OVERFLOW DISPLAY "C OVERFLOW"
               NOT ON OVERFLOW DISPLAY "C NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "C X=[" WX "]".
           MOVE 1 TO WC. MOVE "Z" TO OE (1). MOVE 2 TO WP.
           UNSTRING OG INTO WX WITH POINTER WP
               ON OVERFLOW DISPLAY "B OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "B X=[" WX "] P=" WP.
           STOP RUN.
