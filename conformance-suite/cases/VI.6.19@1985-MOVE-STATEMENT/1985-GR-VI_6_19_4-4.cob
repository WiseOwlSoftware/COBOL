      *>--- conformance-case
      *> id: 1985-GR-VI_6_19_4-4
      *> rules: [1985-GR-VI.6.19.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.4 GR4 (THE MOVE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MOVGR4.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-NUM    PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER
                    VALUE -123.
       01  A-RCV    PIC X(5).
       01  U-RCV    PIC 9(3).
       01  N-RCV    PIC 9(3).
       01  A-SND    PIC X(2) VALUE "12".
       PROCEDURE DIVISION.
       P1.
           MOVE S-NUM TO A-RCV.
           DISPLAY "[" A-RCV "]".
           MOVE S-NUM TO U-RCV.
           DISPLAY U-RCV.
           MOVE A-SND TO N-RCV.
           DISPLAY N-RCV.
           STOP RUN.
