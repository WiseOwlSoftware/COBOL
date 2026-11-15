      *>--- conformance-case
      *> id: g2mov18_cd_de_editing
      *> rules: [2002-GR-14.8.24.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.3 GR3 - "De-editing takes place only when the
      *>   sending operand is a numeric-edited data item and the
      *>   receiving item is a numeric or a numeric-edited data item."
      *>   The operands are communication-section items: CE1, CE2, CS
      *>   (numeric-edited), CN (numeric) and CX (alphanumeric) of the
      *>   record that implicitly redefines the input CD area
      *>   (§2002:A.1.2.2 SR5, 87 positions), and the CD's TEXT LENGTH
      *>   TL (an unsigned 9(4) at positions 75-78, §2002:A.1.2.3
      *>   GR2 h). The items used occupy positions 13-41, apart from SQ
      *>   (1-12) and TL. The facility draws COBOL0903.
      *> D1 CE1 "   123" (ZZ,ZZ9) to WS 9(5): de-edited 123 -> 00123.
      *> D2 CE1 " 1,234" to CE2 (Z,ZZ9), CD edited to CD edited:
      *>    de-edited 1234, re-edited -> "1,234".
      *> D3 CE1 to CN 9(5), CD edited to CD numeric -> 01234.
      *> D4 WS WE " 567" (ZZZ9) to TL -> 0567.
      *> D5 CE1 to CX X(6): the receiver is not numeric, so no
      *>    de-editing: the characters " 1,234" are moved.
      *> D6 CS "-012.50" (-999.99) to WS S9(3)V99 SIGN LEADING
      *>    SEPARATE: the de-edited value is signed (GR4 b)) -> image
      *>    "-01250" (shown through an alphanumeric REDEFINES).
      *> D7 CS to WS X(7): no de-editing -> "-012.50".
      *> Fails if a CD edited sender is not de-edited into a numeric
      *> receiver (D1-D4, D6) or is de-edited into an alphanumeric one
      *> (D5, D7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W5   PIC 9(5).
       01 WE   PIC ZZZ9.
       01 WS5  PIC S9(3)V99 SIGN LEADING SEPARATE.
       01 WS5X REDEFINES WS5 PIC X(6).
       01 WX7  PIC X(7).
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC.
          05 FILLER PIC X(12).
          05 CE1    PIC ZZ,ZZ9.
          05 CE2    PIC Z,ZZ9.
          05 CN     PIC 9(5).
          05 CX     PIC X(6).
          05 CS     PIC -999.99.
          05 FILLER PIC X(46).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 123 TO CE1.
           MOVE CE1 TO W5.
           DISPLAY "D1 CE1=[" CE1 "] W5=" W5.
           MOVE 1234 TO CE1.
           MOVE CE1 TO CE2.
           DISPLAY "D2 CE2=[" CE2 "]".
           MOVE CE1 TO CN.
           DISPLAY "D3 CN=" CN.
           MOVE 567 TO WE.
           MOVE WE TO TL.
           DISPLAY "D4 TL=" TL.
           MOVE CE1 TO CX.
           DISPLAY "D5 CX=[" CX "]".
           MOVE -12.5 TO CS.
           MOVE CS TO WS5.
           DISPLAY "D6 CS=[" CS "] WS5=" WS5X.
           MOVE CS TO WX7.
           DISPLAY "D7 WX7=[" WX7 "]".
           STOP RUN.
