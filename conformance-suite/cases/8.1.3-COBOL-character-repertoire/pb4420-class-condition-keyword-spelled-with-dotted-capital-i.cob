      *>--- conformance-case
      *> id: pb4420-class-condition-keyword-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - NUMERIC in a class condition is a keyword
      *> (8.8.4.4.2, cite.py --check: OK), matched on the basic letters
      *> alone (8.1.3.2 3) a)); NUMERİC, written with U+0130, is a user-
      *> defined word that names no alphabet-name or class-name:
      *> COBOL1639, and it is no reserved word either (it was COBOL0901
      *> as well while the fold mapped U+0130 to i). It fails when the
      *> class-condition arm compares the word through the fold.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XM PIC X(4) VALUE "1234".
       PROCEDURE DIVISION.
           IF XM IS NUMERİC DISPLAY 'Y' END-IF.
           STOP RUN.
