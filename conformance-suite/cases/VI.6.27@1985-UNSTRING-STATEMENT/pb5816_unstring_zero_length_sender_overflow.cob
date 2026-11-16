      *>--- conformance-case
      *> id: pb5816_unstring_zero_length_sender_overflow
      *> rules: [1985-GR-VI.6.27.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5816 - OG is OCCURS 0 TO 8 DEPENDING ON WC (§1985:VI.5.8.3 SR5: integer-1 may be zero) with WC = 0, so as a
      *> sender its size is 0 (§1985:VI.5.8.4 GR3a). §1985:VI.6.27.4 GR17: "An UNSTRING is initiated, and the value in the
      *> data item referenced by identifier-7 is less than 1 or greater than the size of the data item referenced by
      *> identifier-1" is an overflow condition, and GR18 terminates the operation with ON OVERFLOW run. COBOL-85 has no
      *> zero-length-item rule, so leg A (POINTER 1 > size 0) and leg C (the implicit pointer 1) are overflows, WX and the
      *> pointer untouched. Leg B (WC = 1, pointer 2 > size 1) is an overflow too: the control that always held.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5816A.
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
