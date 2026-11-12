      *>--- conformance-case
      *> id: pb5176_move_figurative_binary_receiver
      *> rules: [GR-14.9.25.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5176, WiseOwl COBOL's determination for a figurative constant of category alphanumeric moved to a
      *> numeric item that is not usage DISPLAY (docs/CONFORMANCE.md section 3.10). The move is legal at COBOL-85
      *> (§1985:VI.6.19.4 GR3 e) and "data is moved as if the sending operand were described as an unsigned numeric
      *> integer" (§1985:VI.6.19.4 GR4), but the characters of LOW-VALUES, HIGH-VALUES, QUOTE or a nonnumeric ALL
      *> literal are no digits, so the value left in the receiver is undefined (§1985:VI.6.4.7). WiseOwl COBOL's value
      *> is the one the receiver's storage bytes hold when the fill characters are laid over them, read in the item's
      *> own byte form: LOW-VALUES are all zero bits, so zero, HIGH-VALUES all one bits, so minus one in a signed
      *> binary item, and ALL "AB" the bytes 41 42 hexadecimal, 16706, the raw value of the two bytes with no digit
      *> truncation. Before the determination the program compiled and stopped at run time with an unimplemented-feature exception.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5176B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C4 PIC S9(4) COMP VALUE 7.
       01  P3 PIC S9(5) COMP-3 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           MOVE LOW-VALUES TO C4.
           DISPLAY "LOW " C4.
           MOVE HIGH-VALUES TO C4.
           DISPLAY "HIGH " C4.
           MOVE ALL "AB" TO C4.
           DISPLAY "AB " C4.
           MOVE LOW-VALUES TO P3.
           DISPLAY "PACKED " P3.
           STOP RUN.
