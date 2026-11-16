      *>--- conformance-case
      *> id: pb5816_unstring_zero_length_sender_overflow
      *> rules: [1985-GR-VI.6.27.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5816, PB5858 - OG is OCCURS 0 TO 8 DEPENDING ON WC (§1985:VI.5.8.3 SR5: integer-1 may be zero) with WC = 0, so as a
      *> sender its size is 0 (§1985:VI.5.8.4 GR3a). §1985:VI.6.27.4 GR17a: "An UNSTRING is initiated, and the value in the
      *> data item referenced by identifier-7 is less than 1 or greater than the size of the data item referenced by
      *> identifier-1" is an overflow condition, and GR18 terminates the operation with ON OVERFLOW run. COBOL-85 has no
      *> zero-length-item rule, so leg A (POINTER 1 > size 0) is an overflow, WX and the pointer untouched. Leg B (WC = 1,
      *> pointer 2 > size 1) is an overflow too: the control that always held.
      *> Legs C, D and E have NO POINTER phrase, so there is no identifier-7 for GR17a to test and GR13a examines "beginning
      *> with the leftmost character position". GR17b needs "characters that have not been examined", and a sender of size 0 has
      *> none, so by GR20 NOT ON OVERFLOW runs. GR13: "When the UNSTRING statement is initiated, the current receiving area is
      *> the data item referenced by identifier-4. Data is transferred": the first transfer happens, examining zero characters
      *> (13b: the end of the sender ends the examination), which are an alphanumeric data item MOVEd into the receiving area
      *> (13c), so WX is space filled. A DELIMITER IN item is space filled when the delimiting condition is the end of the
      *> sender (13d), a COUNT IN item receives the number of characters examined, 0 (13e), and the TALLYING item grows by 1
      *> for the one receiving item accessed (GR7, GR16): 5 + 1 = 06. The second receiving item (WY) is never reached (13g).
      *> Leg E: §1985:VI.6.19.4 GR4b, "When a numeric or numeric edited item is the receiving item, alignment by decimal point and any
      *> necessary zero filling takes place as previously defined": the zero characters examined supply no digit, so every digit
      *> position of the receiving item PIC 9(3) is zero filled and WN is 000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5816A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC X(3) VALUE "QQQ".
       01 WY PIC X(3) VALUE "RRR".
       01 WN PIC 9(3) VALUE 123.
       01 WD PIC X(2) VALUE "ZZ".
       01 WK PIC 99 VALUE 55.
       01 WT PIC 99 VALUE 5.
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
           MOVE "QQQ" TO WX.
           UNSTRING OG DELIMITED BY "," INTO WX DELIMITER IN WD
               COUNT IN WK WY TALLYING IN WT
               ON OVERFLOW DISPLAY "D OVERFLOW"
               NOT ON OVERFLOW DISPLAY "D NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "D X=[" WX "] Y=[" WY "] D=[" WD "] K=" WK
               " T=" WT.
           UNSTRING OG INTO WN
               ON OVERFLOW DISPLAY "E OVERFLOW"
               NOT ON OVERFLOW DISPLAY "E NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "E N=" WN.
           MOVE "QQQ" TO WX.
           MOVE 1 TO WC. MOVE "Z" TO OE (1). MOVE 2 TO WP.
           UNSTRING OG INTO WX WITH POINTER WP
               ON OVERFLOW DISPLAY "B OVERFLOW"
               NOT ON OVERFLOW DISPLAY "B NO OVERFLOW"
           END-UNSTRING.
           DISPLAY "B X=[" WX "] P=" WP.
           STOP RUN.
