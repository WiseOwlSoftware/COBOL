*>--- conformance-case
*> id: message-tag-refused-ord-max-argument
*> rules: [AR-15.71.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 15.71.3 rule 1: "Argument-1 shall not be of class boolean, message-tag, object, or
      *> pointer, nor shall it be a strongly-typed group item." (cite.py --check 15.71.3 OK, rule 1.) The ORD-MAX twin of
      *> message-tag-refused-max-argument.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTORDMAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION ORD-MAX(T)
           STOP RUN.
       END PROGRAM MTORDMAX.
