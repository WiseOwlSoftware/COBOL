      *>--- conformance-case
      *> id: g85_bf02_02
      *> rules: [1985-GR-VI.6.8.4-1, 1985-GR-VI.6.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.8.4 GR1 - an arithmetic expression that is a single
      *> identifier (GR1-A, GR1-C) or a single literal (GR1-B, GR1-D) sets
      *> identifier-1 to that value, through numeric, numeric edited and
      *> signed receivers: a wrong value or alignment fails the line.
      *> §1985:VI.6.8.4 GR2 - with several identifier-1 the value is
      *> developed ONCE and stored in each: GR2-A stores A + 1 in A and B
      *> (storing in A first and re-evaluating would give B 07); GR2-B and
      *> GR2-C store one value in receivers of different shapes, each
      *> with its own truncation or ROUNDED. WS-C is shown through a
      *> numeric edited MOVE: DISPLAY's own conversion of an implied
      *> decimal point is the implementor's (§1985:VI.6.10.4 GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0202.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A   PIC 99 VALUE 5.
       01  WS-B   PIC 99 VALUE 0.
       01  WS-C   PIC 9V99 VALUE 0.
       01  WS-D   PIC ZZ9.99.
       01  WS-CE  PIC 9.99.
       01  WS-S   PIC 99 VALUE 42.
       01  WS-N   PIC S9(3) SIGN IS LEADING SEPARATE VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-B = WS-S.
           DISPLAY "GR1-A " WS-B.
           COMPUTE WS-C = 1.25.
           MOVE WS-C TO WS-CE.
           DISPLAY "GR1-B " WS-CE.
           COMPUTE WS-D = WS-S.
           DISPLAY "GR1-C [" WS-D "]".
           COMPUTE WS-N = -17.
           DISPLAY "GR1-D " WS-N.
           COMPUTE WS-A WS-B = WS-A + 1.
           DISPLAY "GR2-A " WS-A " " WS-B.
           COMPUTE WS-B WS-C WS-D = 10 / 4.
           MOVE WS-C TO WS-CE.
           DISPLAY "GR2-B " WS-B " " WS-CE " [" WS-D "]".
           COMPUTE WS-B ROUNDED WS-C = 7 / 2.
           MOVE WS-C TO WS-CE.
           DISPLAY "GR2-C " WS-B " " WS-CE.
           STOP RUN.
