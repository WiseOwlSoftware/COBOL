      *>--- conformance-case
      *> id: pb5924-seg-sort-procs-one-independent-85
      *> rules: [1985-GR-XI.4.4.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.3.3.4 (cite.py: OK): "If a SORT statement appears in a section that is not an independent segment"
      *> A SORT in the fixed portion whose input and output procedures lie wholly in one independent
      *> segment (50) is legal and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGSRT.
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
               INPUT PROCEDURE IS INP
               OUTPUT PROCEDURE IS OUTP.
           DISPLAY "DONE".
           STOP RUN.
       INP SECTION 50.
       IN1.
           MOVE 3 TO K.
           RELEASE SR.
           MOVE 1 TO K.
           RELEASE SR.
           MOVE 2 TO K.
           RELEASE SR.
       OUTP SECTION 50.
       OU1.
           RETURN SW AT END GO TO OU9.
           DISPLAY "K " K.
           GO TO OU1.
       OU9.
           EXIT.
