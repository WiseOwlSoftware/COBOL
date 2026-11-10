      *>--- conformance-case
      *> id: 1989-RV-A_2_16_4-1
      *> rules: [1989-RV-A.2.16.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.16.4 RV1 (THE INTEGER-OF-DATE FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. IODTEST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W PIC 9(6).
000600 PROCEDURE DIVISION.
000700 P1.
000800     COMPUTE W = FUNCTION INTEGER-OF-DATE (16010101).
000900     DISPLAY W.
001000     COMPUTE W = FUNCTION INTEGER-OF-DATE (16020101).
001100     DISPLAY W.
001200     STOP RUN.
