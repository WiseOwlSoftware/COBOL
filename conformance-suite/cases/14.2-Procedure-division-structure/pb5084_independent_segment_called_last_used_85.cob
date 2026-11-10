      *>--- conformance-case
      *> id: pb5084_independent_segment_called_last_used_85
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.1.4.3 resets an independent segment
      *>   on a transfer "from a segment with a different
      *>   segment-number" of the program; a CALL that re-enters a
      *>   program is not one: 1985 X.5.2.4 GR5 "On all other entries
      *>   into the called program, the state of the program ...
      *>   remains unchanged from its state when last exited." The first
      *>   section of PB5084IN is independent
      *>   segment 60, so its second call takes the ALTERed GO TO.
      *>   Expected: "INNER INITIAL" then "INNER LAST-USED". Fails if
      *>   the second CALL returns the segment to its initial state.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084CL.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           CALL "PB5084IN".
           CALL "PB5084IN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084IN.
       PROCEDURE DIVISION.
       IND-SEC SECTION 60.
       SW.
           GO TO P-1.
       P-1.
           DISPLAY "INNER INITIAL".
           ALTER SW TO PROCEED TO P-2.
           EXIT PROGRAM.
       P-2.
           DISPLAY "INNER LAST-USED".
           EXIT PROGRAM.
       END PROGRAM PB5084IN.
       END PROGRAM PB5084CL.
