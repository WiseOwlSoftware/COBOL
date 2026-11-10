      *>--- conformance-case
      *> id: 1985-GR-VI_6_27_4-10
      *> rules: [1985-GR-VI.6.27.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR10 (THE UNSTRING STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNS10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(10) VALUE "AB*#CD%@EF".
       01 DLM PIC X(2) VALUE "%@".
       01 D1 PIC XX.
       01 D2 PIC XX.
       01 D3 PIC XX.
       PROCEDURE DIVISION.
       P1.
           UNSTRING SRC DELIMITED BY "*#" OR DLM
               INTO D1 D2 D3.
           DISPLAY D1.
           DISPLAY D2.
           DISPLAY D3.
           STOP RUN.
