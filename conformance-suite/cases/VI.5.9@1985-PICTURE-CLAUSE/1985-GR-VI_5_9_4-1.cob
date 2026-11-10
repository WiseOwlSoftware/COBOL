      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-1
      *> rules: [1985-GR-VI.5.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR1 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICCAT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ALPHA   PIC AAA VALUE "ABC".
       01  W-NUM     PIC 999 VALUE 123.
       01  W-ALNUM   PIC XXX VALUE "A1B".
       01  W-ALED    PIC XBX.
       01  W-NED     PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO W-ALED.
           MOVE 45 TO W-NED.
           DISPLAY W-ALPHA.
           DISPLAY W-NUM.
           DISPLAY W-ALNUM.
           DISPLAY W-ALED.
           DISPLAY W-NED.
           STOP RUN.
