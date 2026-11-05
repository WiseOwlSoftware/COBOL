      *>--- conformance-case
      *> id: pb4652-relation-self-outside-method-position
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.4.3.8.3 SR1 - This identifier format may be specified only in a method definition.
      *> SELF written as a relation operand in a PROGRAM is refused at the word (kb/Work PB4652),
      *> not at the IF keyword that opens the statement.
      *> The word sits at (line 18, column 19) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           IF O = SELF DISPLAY 'Y' END-IF.
           STOP RUN.
