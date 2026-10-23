*>--- conformance-case
*> id: pb3563_options_initialize_redefined_numeric_leaf
*> rules: [GR-14.6.2.3.2-1, GR-11.9.10.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3563 (review round 2) - ISO 14.6.2.3.2 GR1 (cite.py OK: "the storage allocated for the implied or associated sections is set to the specified-fill-character")
      *> reaches a numeric item that is the canonical member of a REDEFINES class exactly as it reaches any other numeric item (owner decision kb/Work R53).
      *> OPTIONS INITIALIZE WORKING-STORAGE TO X"5A".
      *>   RE-B   PIC 9(3), redefined by RE-B2 -> stores its three storage characters "ZZZ", so it is NOT NUMERIC (8.8.4.4.4 GR3 c: the content is not a valid representation)
      *>   BN-A   PIC 9(4) COMP-5, redefined by BN-B -> two storage bytes X"5A5A" = 23130
      *>   PL-B   PIC 9(3), no REDEFINES (the control that always conformed) -> "ZZZ"
      *> BEFORE: the class member stayed a native long while the emitter seeded the fill string -> error CS0029, legal source failed to compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3563OIR.
       OPTIONS.
           INITIALIZE WORKING-STORAGE TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RE.
          05 RE-B PIC 9(3).
          05 RE-B2 REDEFINES RE-B PIC 9V99.
       01 BN.
          05 BN-A PIC 9(4) COMP-5.
          05 BN-B REDEFINES BN-A PIC 9(4) COMP-5.
       01 PL.
          05 PL-B PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "RE=[" RE "]".
           IF RE-B IS NUMERIC DISPLAY "RE-B NUMERIC" ELSE DISPLAY "RE-B NOT NUMERIC" END-IF.
           DISPLAY "BN=" BN-A " " BN-B.
           DISPLAY "PL=[" PL "]".
           STOP RUN.
