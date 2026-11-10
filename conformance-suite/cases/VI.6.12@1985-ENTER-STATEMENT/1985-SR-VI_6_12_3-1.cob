      *>--- conformance-case
      *> id: 1985-SR-VI_6_12_3-1
      *> rules: [1985-SR-VI.6.12.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.3 SR1 (THE ENTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ENTTST.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     ENTER COBOL.
000600     DISPLAY "DONE".
000700     STOP RUN.
