      *>--- conformance-case
      *> id: g2-class-boolean-on-numeric
      *> rules: [2002-SR-8.8.4.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.3.2 SR4: "BOOLEAN shall not be specified if the
      *> category of the data item referenced by identifier-1 is
      *> numeric or numeric-edited."      cite.py: OK §2002 SR4
      *> A prohibiting syntax rule that grants no form, so a refusal
      *> is its evidence. This case is the numeric leg; the operand
      *> is usage display, so §2002:8.8.4.1.3.2 SR3 (usage display or
      *> national) does not bar it - only SR4 does. The case fails if
      *> the class test compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLB01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OP PIC 9(4) VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF OP IS BOOLEAN DISPLAY "B" END-IF
           STOP RUN.
