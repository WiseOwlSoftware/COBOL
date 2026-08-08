      *> kb/Work R32 - COMPILE-ONLY control (no .out): a name DECLARED in the SCREEN SECTION is not
      *> "undefined". The documented COBOLNET1560 posture (docs/CONFORMANCE.md section 4) is
      *> compile-accept with no screen behavior, so a reference to SG must compile (the runtime
      *> reference stays a staged loud naming the screen-section cause). R30's demanding resolver
      *> briefly drew COBOLNET1639 here - the differential's syn_screen:221 flip.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SCRREF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       SCREEN SECTION.
       01 SG.
          05 SI1 LINE 1 COL 1 PIC X FROM X.
       PROCEDURE DIVISION.
           DISPLAY SG END-DISPLAY.
           STOP RUN.
