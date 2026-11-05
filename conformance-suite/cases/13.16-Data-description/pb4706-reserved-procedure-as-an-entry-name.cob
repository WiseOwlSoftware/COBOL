      *>--- conformance-case
      *> id: pb4706-reserved-procedure-as-an-entry-name
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  8.3.2.1 1) "Reserved words shall not be used as user-defined words or system-names" and 8.9 reserves
      *> PROCEDURE at every edition, so an entry-name PROCEDURE is refused.  The grammar held a dataName alternative for it,
      *> which accepted this program; the NC205A item it was kept for continues on its next line as one word, PROCEDUREDIVISION.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PROCEDURE PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
