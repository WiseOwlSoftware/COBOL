      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-4
      *> rules: [1985-GR-IV.6.4.2.1.1.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR4 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SIZETST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-N  PIC 9 VALUE 9.
000600 01  WS-M  PIC 9 VALUE 1.
000700 PROCEDURE DIVISION.
000800 MAIN-PARA.
000900     ADD 1 TO WS-N
001000         ON SIZE ERROR DISPLAY "SIZE ERROR"
001100         NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
001200     END-ADD.
001300     DISPLAY WS-N.
001400     ADD 1 TO WS-M
001500         ON SIZE ERROR DISPLAY "SIZE ERROR"
001600         NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR"
001700     END-ADD.
001800     DISPLAY WS-M.
001900     STOP RUN.
