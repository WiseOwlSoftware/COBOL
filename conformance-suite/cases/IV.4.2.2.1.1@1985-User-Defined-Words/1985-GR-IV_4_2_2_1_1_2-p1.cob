      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_1_2-p1
      *> rules: [1985-GR-IV.4.2.2.1.1.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1.2 p1 (User-Defined Words). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.1.2-p1: a mnemonic-name assigns a user word
      * to an implementor-name in SPECIAL-NAMES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P02MNEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS MY-TERMINAL
           SYSOUT IS MY-PRINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "VIA-CONSOLE" UPON MY-TERMINAL.
           DISPLAY "VIA-SYSOUT" UPON MY-PRINTER.
           STOP RUN.
