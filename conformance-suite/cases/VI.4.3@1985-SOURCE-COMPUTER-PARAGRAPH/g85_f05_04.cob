      *>--- conformance-case
      *> id: g85_f05_04
      *> rules: [1985-GR-VI.4.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.4 GR3 - the SOURCE-COMPUTER paragraph is written
      *> with no source-computer-entry (the general format brackets the
      *> entry); the compiling computer is the source computer, so the
      *> program translates and runs here. A compiler that requires a
      *> computer-name after SOURCE-COMPUTER fails to translate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0504.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER.
       OBJECT-COMPUTER. G85-OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "EMPTY SOURCE-COMPUTER " N.
           STOP RUN.
