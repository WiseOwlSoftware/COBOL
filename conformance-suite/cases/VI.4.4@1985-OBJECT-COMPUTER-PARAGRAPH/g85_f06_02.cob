      *>--- conformance-case
      *> id: g85_f06_02
      *> rules: [1985-GR-VI.4.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR4 - the OBJECT-COMPUTER paragraph is
      *>   specified with no object-computer-entry (the format of
      *>   §1985:VI.4.4.2 brackets the whole entry, computer-name
      *>   included); the object computer is then the implementor's.
      *>   The program must translate and run: the paragraph header
      *>   alone is a complete OBJECT-COMPUTER paragraph.
      *> A leg fails if the bare header is refused or if the program
      *> does not run to its last DISPLAY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0602.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER.
       OBJECT-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(3) VALUE 40.
       PROCEDURE DIVISION.
       P-MAIN.
           ADD 2 TO WS-N.
           DISPLAY "OBJECT-COMPUTER HEADER ONLY".
           DISPLAY "N=" WS-N.
           STOP RUN.
