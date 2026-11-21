      *>--- conformance-case
      *> id: pb5924-sysname-underscore-85
      *> rules: [1985-SR-IV.4.2.2.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.2 (cite.py: OK): "each character used in the formation of a system-name"
      *> The underscore is not in the 1985 set of characters of a
      *> system-name: MY_BOX is refused at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SYSNMU.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MY_BOX.
       OBJECT-COMPUTER. MY_BOX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
