      *>--- conformance-case
      *> id: 1985-GR-VI_6_25_4-9
      *> rules: [1985-GR-VI.6.25.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR9 (THE STRING STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STROVF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-OUT  PIC X(5)  VALUE "*****".
       01  WS-PTR  PIC 99    VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABCDEFG" DELIMITED BY SIZE
               INTO WS-OUT WITH POINTER WS-PTR
               ON OVERFLOW DISPLAY "OVERFLOW1"
               NOT ON OVERFLOW DISPLAY "NO OVERFLOW1"
           END-STRING.
           DISPLAY WS-OUT.
           DISPLAY WS-PTR.
           MOVE "*****" TO WS-OUT.
           MOVE 0 TO WS-PTR.
           STRING "XY" DELIMITED BY SIZE
               INTO WS-OUT WITH POINTER WS-PTR
               ON OVERFLOW DISPLAY "OVERFLOW2"
               NOT ON OVERFLOW DISPLAY "NO OVERFLOW2"
           END-STRING.
           DISPLAY WS-OUT.
           DISPLAY WS-PTR.
           STOP RUN.
