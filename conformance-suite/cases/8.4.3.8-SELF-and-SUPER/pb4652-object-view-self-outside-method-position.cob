      *>--- conformance-case
      *> id: pb4652-object-view-self-outside-method-position
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.4.3.8.3 SR1 - SELF as the identifier-1 of an object-view in a PROGRAM is refused at
      *> the word SELF (kb/Work PB4652), not at the statement that holds the view.
      *> The word sits at (line 17, column 15) of the statement below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4652C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
           IF SELF AS UNIVERSAL = O DISPLAY 'Y' END-IF.
           STOP RUN.
