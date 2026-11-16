      *>--- conformance-case
      *> id: g85_bf13_03
      *> rules: [1985-GR-VI.6.10.4-1, 1985-GR-VI.6.10.4-4, 1985-GR-VI.6.10.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.10.4 GR1 - the operands are transferred in the order
      *> listed (line 1: literal, item, literal, item, literal).
      *> §1985:VI.6.10.4 GR4 - the console receives a 5-character item
      *> whole, trailing spaces included (line 2: [AB   ]).
      *> §1985:VI.6.10.4 GR9 - without WITH NO ADVANCING the device is
      *> reset to the start of the next line after the last operand:
      *> every DISPLAY here begins a new line, and the one after the
      *> NO ADVANCING pair (XY) starts on its own line again.
      *> A leg fails if operands are reordered or separated, if trailing
      *> spaces are trimmed, or if two DISPLAYs share a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1303.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A                 PIC X(3) VALUE "ABC".
       01  W-N                 PIC 9(4) VALUE 42.
       01  W-B                 PIC X(5) VALUE "AB".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "<" W-A ">" W-N "<".
           DISPLAY "[" W-B "]".
           DISPLAY W-A.
           DISPLAY "X" WITH NO ADVANCING.
           DISPLAY "Y".
           DISPLAY "END".
           STOP RUN.
