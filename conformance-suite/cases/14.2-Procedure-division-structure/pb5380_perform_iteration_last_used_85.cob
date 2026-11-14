      *>--- conformance-case
      *> id: pb5380_perform_iteration_last_used_85
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5380 - 1985 XVI.1.4.3 puts an independent segment in
      *>   its last-used state when "Control is transferred implicitly
      *>   to that segment from a segment with a different
      *>   segment-number". Under an iterative PERFORM each execution
      *>   of the range after the first is entered from the control
      *>   mechanism of the PERFORM (1985 IV.4.4.2 GR1), an implicit
      *>   transfer, so the ALTER of the earlier execution stands. The
      *>   first execution is entered by the PERFORM statement itself,
      *>   an explicit transfer: initial. TIMES, UNTIL and VARYING.
      *>   Expected: S60 INITIAL, S60 ALTERED, S60 ALTERED (TIMES);
      *>   S60 INITIAL, S60 ALTERED (UNTIL, a new PERFORM statement);
      *>   T70 INITIAL 1, T70 ALTERED 2 (VARYING). Fails if a later
      *>   iteration resets the segment (a second INITIAL in a row).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5380IT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 0.
       M1.
           DISPLAY "TIMES".
           PERFORM S60 3 TIMES.
           DISPLAY "UNTIL".
           MOVE 0 TO K.
           PERFORM S60 UNTIL K > 1.
           DISPLAY "VARYING".
           PERFORM T70 VARYING K FROM 1 BY 1 UNTIL K > 2.
           STOP RUN.
       S60 SECTION 60.
       G1.
           GO TO A1.
       A1.
           DISPLAY "S60 INITIAL".
           ALTER G1 TO PROCEED TO B1.
           GO TO E1.
       B1.
           DISPLAY "S60 ALTERED".
       E1.
           ADD 1 TO K.
       T70 SECTION 70.
       G2.
           GO TO A2.
       A2.
           DISPLAY "T70 INITIAL " K.
           ALTER G2 TO PROCEED TO B2.
           GO TO E2.
       B2.
           DISPLAY "T70 ALTERED " K.
       E2.
           EXIT.
