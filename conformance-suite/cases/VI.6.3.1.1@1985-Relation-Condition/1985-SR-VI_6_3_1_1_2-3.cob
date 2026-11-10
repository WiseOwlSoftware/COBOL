      *>--- conformance-case
      *> id: 1985-SR-VI_6_3_1_1_2-3
      *> rules: [1985-SR-VI.6.3.1.1.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.2 SR3 (Relation Condition). A refusal: the
      *> rule forbids line 11 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2532 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELCOND.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM-A    PIC 9V9 VALUE 1.5.
       01  ALF-B    PIC XX VALUE "15".
       01  NUM-I    PIC 99 VALUE 15.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NUM-A = ALF-B
               DISPLAY "EQUAL"
           END-IF.
           STOP RUN.
