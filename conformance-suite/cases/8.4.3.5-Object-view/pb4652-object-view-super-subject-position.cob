      *>--- conformance-case
      *> id: pb4652-object-view-super-subject-position
      *> rules: [SR-8.4.3.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.4.3.5.3 SR1 - Identifier-1 shall be of class object; the predefined object references
      *> SUPER and NULL shall not be specified. The refusal is about the word SUPER and is reported
      *> there (kb/Work PB4652), not at the statement.
      *> The word sits at (line 18, column 15) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           IF SUPER AS UNIVERSAL = O DISPLAY 'Y' END-IF.
           STOP RUN.
