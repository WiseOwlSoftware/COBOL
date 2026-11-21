      *>--- conformance-case
      *> id: pb5924-seg-sort-procs-two-independent-85
      *> rules: [1985-GR-XI.4.4.4-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.3.3.4 (cite.py: OK): "If a SORT statement appears in a section that is not an independent segment"
      *> A SORT in the fixed portion whose input procedure spans independent segments 50 and 60: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGSRB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "SORTWK".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K PIC 9.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SW ON ASCENDING KEY K
               INPUT PROCEDURE IS INA THRU INB
               OUTPUT PROCEDURE IS OUTP.
           STOP RUN.
       INA SECTION 50.
       IA1.
           MOVE 3 TO K.
           RELEASE SR.
       INB SECTION 60.
       IB1.
           MOVE 1 TO K.
           RELEASE SR.
       OUTP SECTION.
       OU1.
           RETURN SW AT END GO TO OU9.
           DISPLAY "K " K.
           GO TO OU1.
       OU9.
           EXIT.
