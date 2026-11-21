      *>--- conformance-case
      *> id: pb5924-sysname-trailing-hyphen-85
      *> rules: [1985-SR-IV.4.2.2.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.2 (cite.py: OK): "each character used in the formation of a system-name"
      *> A computer-name ending in a hyphen is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SYSNMH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ABC-.
       OBJECT-COMPUTER. ABC-.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
