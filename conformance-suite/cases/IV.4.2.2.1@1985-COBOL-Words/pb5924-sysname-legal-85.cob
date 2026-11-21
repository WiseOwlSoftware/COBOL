      *>--- conformance-case
      *> id: pb5924-sysname-legal-85
      *> rules: [1985-SR-IV.4.2.2.1.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.2 (cite.py: OK): "each character used in the formation of a system-name"
      *> A computer-name of letters, digits and an inner hyphen is
      *> accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SYSNML.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-370X9.
       OBJECT-COMPUTER. IBM-370X9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
