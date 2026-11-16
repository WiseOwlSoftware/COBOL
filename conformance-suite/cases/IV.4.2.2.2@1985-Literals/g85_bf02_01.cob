      *>--- conformance-case
      *> id: g85_bf02_01
      *> rules: [1985-GR-IV.4.2.2.2.3-1, 1985-GR-IV.4.2.2.2.3-L2.2, 1985-GR-IV.4.2.2.2.3-L2.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR1 - ZERO, ZEROS and ZEROES are the numeric
      *> value 0 (GR1-A, GR1-B, GR1-C, GR1-D) and one or more '0'
      *> characters of an alphanumeric item (GR1-E, GR1-F); a wrong value,
      *> a non-zero digit or a short fill fails the line.
      *> §1985:IV.4.2.2.2.3 GRL2.2 - a figurative constant other than ALL
      *> literal in DISPLAY (L22-A), STRING (L22-B) and UNSTRING (L22-C)
      *> is ONE character: a longer string widens the brackets, shifts
      *> the STRING result or swallows the empty UNSTRING field.
      *> §1985:IV.4.2.2.2.3 GRL2.3 - ALL literal not associated with a
      *> data item has the length of the literal (L23-A, L23-B): a repeat
      *> or a one-character cut fails the line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NUM    PIC 9(3) VALUE 123.
       01  WS-ED     PIC ZZ9.
       01  WS-ALN    PIC X(4) VALUE "ABCD".
       01  WS-ALN5   PIC X(5) VALUE "ABCDE".
       01  WS-RES    PIC X(10).
       01  WS-SRC    PIC X(6) VALUE "AB  CD".
       01  WS-R1     PIC X(3) VALUE "***".
       01  WS-R2     PIC X(3) VALUE "***".
       01  WS-R3     PIC X(3) VALUE "***".
       01  WS-TALLY  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ZERO TO WS-NUM.
           DISPLAY "GR1-A " WS-NUM.
           MOVE 7 TO WS-NUM.
           ADD ZEROS TO WS-NUM.
           DISPLAY "GR1-B " WS-NUM.
           MOVE ZEROES TO WS-ED.
           DISPLAY "GR1-C [" WS-ED "]".
           IF WS-NUM > ZERO
               DISPLAY "GR1-D GT"
           ELSE
               DISPLAY "GR1-D NOT-GT"
           END-IF.
           MOVE ZEROS TO WS-ALN.
           DISPLAY "GR1-E " WS-ALN.
           MOVE ALL ZERO TO WS-ALN5.
           DISPLAY "GR1-F " WS-ALN5.
           DISPLAY "L22-A [" ZERO "][" SPACE "][" QUOTES "]".
           MOVE ALL "." TO WS-RES.
           STRING "<" SPACES ">" ZEROS "<" DELIMITED BY SIZE
               INTO WS-RES.
           DISPLAY "L22-B " WS-RES.
           UNSTRING WS-SRC DELIMITED BY SPACES
               INTO WS-R1 WS-R2 WS-R3 TALLYING IN WS-TALLY.
           DISPLAY "L22-C [" WS-R1 "][" WS-R2 "][" WS-R3 "] " WS-TALLY.
           DISPLAY "L23-A [" ALL "AB" "]".
           DISPLAY "L23-B [" ALL "XYZ" "]".
           STOP RUN.
