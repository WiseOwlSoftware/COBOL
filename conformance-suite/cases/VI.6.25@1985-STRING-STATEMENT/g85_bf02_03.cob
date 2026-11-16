      *>--- conformance-case
      *> id: g85_bf02_03
      *> rules: [1985-GR-VI.6.25.4-2, 1985-GR-VI.6.25.4-3, 1985-GR-VI.6.25.4-4, 1985-GR-VI.6.25.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR2 - SIZE moves the complete sending item (S1);
      *> literal-2 (S2) and identifier-2 (S3, two characters) delimit the
      *> move; a figurative constant delimiter is ONE character (S4: SPACE
      *> stops at the first space, ZERO at the first '0').
      *> §1985:VI.6.25.4 GR3 - a figurative constant as literal-1 is one
      *> DISPLAY character (S5).
      *> §1985:VI.6.25.4 GR4 - a: no space filling (every line keeps the
      *> '*' fill); b: transfer stops at the delimiter, which is not
      *> transferred (S2, S3, S4) or at the end of the receiver (S6B);
      *> c: SIZE transfers until the receiver ends (S6A); the senders are
      *> processed in order (S2, S4, S5).
      *> §1985:VI.6.25.4 GR8 - only the positions referenced change: the
      *> '*' tails and, with POINTER, the positions before and after the
      *> transferred ones (S7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0203.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-R     PIC X(12).
       01  WS-R4    PIC X(4).
       01  WS-SZ    PIC X(5) VALUE "AB CD".
       01  WS-SP    PIC X(8) VALUE "AB CD".
       01  WS-ZD    PIC X(7) VALUE "1203004".
       01  WS-DL    PIC XX VALUE "CD".
       01  WS-SRC2  PIC X(6) VALUE "ABCDEF".
       01  WS-P     PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ALL "*" TO WS-R.
           STRING WS-SZ DELIMITED BY SIZE INTO WS-R.
           DISPLAY "S1 [" WS-R "]".
           MOVE ALL "*" TO WS-R.
           STRING "XY/Z" DELIMITED BY "/"
                  "QR" DELIMITED BY SIZE INTO WS-R.
           DISPLAY "S2 [" WS-R "]".
           MOVE ALL "*" TO WS-R.
           STRING WS-SRC2 DELIMITED BY WS-DL INTO WS-R.
           DISPLAY "S3 [" WS-R "]".
           MOVE ALL "*" TO WS-R.
           STRING WS-SP DELIMITED BY SPACE
                  "|" DELIMITED BY SIZE
                  WS-ZD DELIMITED BY ZERO INTO WS-R.
           DISPLAY "S4 [" WS-R "]".
           MOVE ALL "*" TO WS-R.
           STRING "A" SPACE "B" QUOTE "C" ZERO "D"
                  DELIMITED BY SIZE INTO WS-R.
           DISPLAY "S5 [" WS-R "]".
           MOVE ALL "*" TO WS-R4.
           STRING WS-SRC2 DELIMITED BY SIZE INTO WS-R4.
           DISPLAY "S6A [" WS-R4 "]".
           MOVE ALL "*" TO WS-R4.
           STRING "WX" DELIMITED BY "/"
                  WS-SRC2 DELIMITED BY "Z" INTO WS-R4.
           DISPLAY "S6B [" WS-R4 "]".
           MOVE "1234567890AB" TO WS-R.
           MOVE 3 TO WS-P.
           STRING "XY" DELIMITED BY SIZE INTO WS-R WITH POINTER WS-P.
           DISPLAY "S7 [" WS-R "] " WS-P.
           STOP RUN.
