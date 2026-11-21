      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_2-1
      *> rules: [1985-GR-IV.4.2.2.1.2-1, 1985-GR-IV.4.2.2.1.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.2 GR1; §1985:IV.4.2.2.1.2 GR2 (COBOL Words).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.2-1..3: computer-name, implementor-name
      * and language-name are system-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P04SYS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MY-COMPUTER-2.
       OBJECT-COMPUTER. MY-COMPUTER-2.
       SPECIAL-NAMES.
           CONSOLE IS TERM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SYSNAMES-OK" UPON TERM.
           STOP RUN.
