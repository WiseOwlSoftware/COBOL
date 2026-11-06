      *>--- conformance-case
      *> id: pb4788-header-out-of-order-after-a-screen-entrys-level-number
      *> rules: [FMT-13.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4788.  13.2.1 prints LINKAGE SECTION before the SCREEN SECTION, which is last, so no period lets LINKAGE
      *> SECTION follow the screen section: the level-number is a dangling entry and the header is out of order.  It is the
      *> report 01 FILE SECTION. draws in the working-storage section (COBOL0001), never the syntax-kind refusal COBOL5506
      *> that listed every clause of the screen entry as "expected".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4788S.
       DATA DIVISION.
       SCREEN SECTION.
       01 LINKAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
