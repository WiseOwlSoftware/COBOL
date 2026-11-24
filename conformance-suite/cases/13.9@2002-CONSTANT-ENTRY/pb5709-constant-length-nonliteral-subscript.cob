*>--- conformance-case
*> id: pb5709-constant-length-nonliteral-subscript
*> rules: [2002-SR-13.9.2-3, 2002-GR-13.9.3-5, 2002-GR-13.9.3-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> The 2002 twin of 13.10-Constant/pb1232-constant-length-noninteger-subscript (kb/Work PB1232, PB5709). The 2002
      *> constant entry has no literal-subscript rule (13.9.2 SR3 allows only qualification), so a subscript is an ordinary
      *> one (8.4.1.2.2): an arithmetic expression, with no syntax rule on its class (a non-integer value is a run-time
      *> condition, GR1b). The length never depends on the subscript (13.9.3 GR5/GR6), so the entry is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5709NL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 2.
       01 KD CONSTANT AS 1.5.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K1 CONSTANT AS LENGTH OF E (KD).
       01 K2 CONSTANT AS LENGTH OF E (FUNCTION MIN (I J)).
       PROCEDURE DIVISION.
           DISPLAY "K1=" K1 " K2=" K2
           STOP RUN.
